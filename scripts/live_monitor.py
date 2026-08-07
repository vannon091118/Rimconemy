#!/usr/bin/env python3
"""
live_monitor.py — Rimconemy Live Player.log Monitor

Tails the RimWorld Player.log during gameplay, filters out noise,
and shows a structured, colorized real-time dashboard of:
  - Test suite results (pass/fail counts per suite)
  - TEST-FAIL details with file:line
  - Errors, warnings, anomalies
  - Bootstrap status

Usage:
  python3 scripts/live_monitor.py                    # auto-detect Player.log
  python3 scripts/live_monitor.py --log <path>       # custom log path
  python3 scripts/live_monitor.py --level 2          # verbosity: 1=compact, 2=full, 3=debug

Controls:
  Ctrl+C — quit

Design:
  - Reads new lines from Player.log every 0.5s (no polling overhead)
  - Detects RimWorld log rotation (Player.log, Player-prev.log)
  - Filters: [Rimconemy.*] lines + critical vanilla errors
  - Suppresses: UnityEngine fallback spam, known non-critical warnings
  - Colorized: green=PASS, red=FAIL/ERROR, yellow=WARN, cyan=INFO
"""

import os
import re
import sys
import time
import signal
from datetime import datetime
from pathlib import Path
from collections import defaultdict

# ── Config ─────────────────────────────────────────────────────
DEFAULT_LOG = os.path.expanduser(
    "~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
)
COLORS = sys.stdout.isatty()  # auto-detect terminal

# ── Color helpers ──────────────────────────────────────────────
def _c(code, text):
    if COLORS:
        return f"\033[{code}m{text}\033[0m"
    return text

def green(t):  return _c("32", t)
def red(t):    return _c("31", t)
def yellow(t): return _c("33", t)
def cyan(t):   return _c("36", t)
def bold(t):   return _c("1", t)
def dim(t):    return _c("2", t)
def magenta(t): return _c("35", t)

# ── Pattern definitions ─────────────────────────────────────────
RX_RIMCONEMY = re.compile(r"^\[Rimconemy\.([^\]]+)\] (.+)$")
RX_TEST_SUMMARY = re.compile(
    r"(?P<suite>[A-Za-z0-9_\- )(]+?) (?:tests?|regression)(?: \([^)]+\))?: "
    r"(?P<passed>\d+)(?:/(?P<total>\d+))? passed, (?P<failed>\d+) failed"
)
RX_TEST_FAIL = re.compile(r"(?:test FAILED:?|TEST-FAIL)\s*(.+)", re.IGNORECASE)
RX_BOOTSTRAP = re.compile(r"bootstrap (START|COMPLETE)")
RX_PROFILE = re.compile(r"Profile detected: (\w+)")
RX_PACKAGE = re.compile(r"Package registered: (rimconemy\.\w+) v([\d.]+)")
RX_CROSSREF = re.compile(r"Could not resolve cross-reference to (\S+) named (\S+)")
RX_ERROR = re.compile(r"(?:Error|Exception|FATAL)", re.IGNORECASE)
RX_WARNING = re.compile(r"Warning", re.IGNORECASE)
RX_DEFERRED = re.compile(r"TEST-DEFERRED (.+)")

# Vanilla noise to suppress
NOISE_PATTERNS = [
    re.compile(r"^Fallback handler could not load", re.IGNORECASE),
    re.compile(r"^\[UnityEngine\].*", re.IGNORECASE),
    re.compile(r"^UnloadTime:", re.IGNORECASE),
    re.compile(r"^\(Filename:", re.IGNORECASE),
    re.compile(r"^Setting up [0-9]+ worker threads", re.IGNORECASE),
]

# Critical vanilla patterns to keep
CRITICAL_VANILLA = [
    re.compile(r"NullReferenceException", re.IGNORECASE),
    re.compile(r"XML error:", re.IGNORECASE),
    re.compile(r"Could not resolve cross-reference", re.IGNORECASE),
    re.compile(r"Config error in", re.IGNORECASE),
]


class LiveMonitor:
    def __init__(self, log_path: str, level: int = 1):
        self.log_path = log_path
        self.level = level
        self.position = 0
        self.lines_seen = 0
        self.start_time = time.time()

        # State
        self.bootstrap_complete = False
        self.packages = {}
        self.profiles = []
        self.suites = {}          # suite_key → {passed, failed, failures[], status}
        self.errors = []
        self.warnings = []
        self.cross_refs = []
        self.anomalies = []
        self.last_suite = None
        self.pending_failures = []

        # Dashboard refresh
        self.last_dashboard = 0

    def _should_keep(self, line: str) -> bool:
        """Filter: keep only relevant lines, suppress noise."""
        # Always keep Rimconemy lines
        if line.startswith("[Rimconemy."):
            return True
        # Keep critical vanilla errors
        for pat in CRITICAL_VANILLA:
            if pat.search(line):
                return True
        # Suppress known noise
        for pat in NOISE_PATTERNS:
            if pat.search(line):
                return False
        # Level 3: show everything except obvious noise
        if self.level >= 3:
            return True
        return False

    def _process_line(self, line: str):
        """Parse a single log line and update state."""
        stripped = line.strip()

        # ── Vanilla critical ──
        for pat in CRITICAL_VANILLA:
            if pat.search(stripped) and not stripped.startswith("[Rimconemy."):
                self.anomalies.append({"type": "vanilla_critical", "line": stripped[:200]})
                return

        # ── Rimconemy lines ──
        m = RX_RIMCONEMY.match(stripped)
        if not m:
            return
        pkg = m.group(1)
        msg = m.group(2)

        # Bootstrap
        if RX_BOOTSTRAP.search(msg):
            if "COMPLETE" in msg:
                self.bootstrap_complete = True
            return

        pm = RX_PROFILE.search(msg)
        if pm:
            self.profiles.append(pm.group(1))
            return

        pkm = RX_PACKAGE.search(msg)
        if pkm:
            self.packages[pkm.group(1)] = pkm.group(2)
            return

        # Test failures (buffer until suite summary)
        tfm = RX_TEST_FAIL.search(msg)
        if tfm:
            self.pending_failures.append(tfm.group(1).strip())
            if self.level >= 1:
                print(f"  {red('✗ TEST-FAIL')} {dim(pkg)} {tfm.group(1).strip()[:120]}")
            return

        # Deferred suites
        dm = RX_DEFERRED.search(msg)
        if dm:
            if self.level >= 1:
                print(f"  {yellow('⊘ DEFERRED')} {dim(pkg)} {dm.group(1)[:120]}")
            return

        # Test suite summary
        tsm = RX_TEST_SUMMARY.search(msg)
        if tsm:
            suite_name = tsm.group("suite").strip()
            passed = int(tsm.group("passed"))
            failed = int(tsm.group("failed"))
            key = f"{pkg}/{suite_name}"

            # Dedup: keep highest total
            total = passed + failed
            if key in self.suites:
                old_total = self.suites[key]["passed"] + self.suites[key]["failed"]
                if total <= old_total:
                    return  # keep old (likely the real TestSuite one)

            self.suites[key] = {
                "package": pkg,
                "suite": suite_name,
                "passed": passed,
                "failed": failed,
                "failures": list(self.pending_failures),
                "time": time.time(),
            }
            self.pending_failures.clear()

            # Print suite result (skip legacy 0/0 noise)
            if total > 0:
                if failed > 0:
                    icon = red(f"✗ {failed} FAILS")
                else:
                    icon = green(f"✓ {passed} ok")
                if self.level >= 1:
                    print(f"  {icon}  {bold(suite_name)}  {dim(pkg)}")
            return

        # Errors & warnings
        if RX_ERROR.search(msg):
            self.errors.append({"pkg": pkg, "msg": msg, "time": time.time()})
            if self.level >= 1:
                print(f"  {red('● ERROR')} [{pkg}] {msg[:160]}")
            return

        if RX_WARNING.search(msg):
            self.warnings.append({"pkg": pkg, "msg": msg, "time": time.time()})
            if self.level >= 2:
                print(f"  {yellow('● WARN')}  [{pkg}] {msg[:160]}")
            return

        # Cross-ref
        crm = RX_CROSSREF.search(msg)
        if crm:
            self.cross_refs.append({"type": crm.group(1), "name": crm.group(2)})
            if self.level >= 1:
                print(f"  {magenta('↗ CROSS-REF')} {crm.group(1)} › {crm.group(2)}")
            return

        # Level 3: show everything else
        if self.level >= 3:
            print(f"  {dim('·')} [{pkg}] {msg[:160]}")

    def _render_dashboard(self):
        """Print a compact status dashboard."""
        elapsed = time.time() - self.start_time
        total_passed = sum(s["passed"] for s in self.suites.values())
        total_failed = sum(s["failed"] for s in self.suites.values())
        failed_suites = [s for s in self.suites.values() if s["failed"] > 0]

        # Clear and redraw header
        sys.stdout.write("\033[2J\033[H")  # clear screen, cursor home

        print(bold("╔══════════════════════════════════════════════════════════════╗"))
        print(bold("║") + bold("  Rimconemy Live Monitor").center(62) + bold("║"))
        print(bold("╠══════════════════════════════════════════════════════════════╣"))

        # Status line
        boot_icon = green("✓") if self.bootstrap_complete else yellow("…")
        pkg_count = len(self.packages)
        profile_str = ", ".join(list(dict.fromkeys(self.profiles))[:2]) if self.profiles else "—"

        status = f"  Boot: {boot_icon} | {pkg_count} packages | {profile_str}"
        print(bold("║") + status.ljust(62) + bold("║"))

        # Test summary
        if total_failed > 0:
            test_line = f"  Tests: {green(str(total_passed))} passed, {red(str(total_failed))} FAILED, {len(self.suites)} suites"
        else:
            test_line = f"  Tests: {green(f'{total_passed} passed')}, {len(self.suites)} suites"
        print(bold("║") + test_line.ljust(62) + bold("║"))

        error_count = len(self.errors)
        warn_count = len(self.warnings)
        anom_count = len(self.anomalies)
        extra = f"  Errors: {error_count} | Warnings: {warn_count} | Anomalies: {anom_count}"
        print(bold("║") + extra.ljust(62) + bold("║"))

        # Failed suites details
        if failed_suites:
            print(bold("╠══════════════════════════════════════════════════════════════╣"))
            print(bold("║") + red("  FAILED SUITES:").ljust(62) + bold("║"))
            for s in failed_suites[:8]:
                line = f"    {s['package']}/{s['suite']}: {s['failed']} failures"
                print(bold("║") + red(line).ljust(62) + bold("║"))

        # Recent anomalies
        if self.anomalies:
            print(bold("╠══════════════════════════════════════════════════════════════╣"))
            print(bold("║") + yellow("  ANOMALIES:").ljust(62) + bold("║"))
            for a in self.anomalies[-5:]:
                line = f"    {a['type']}: {a['line'][:50]}"
                print(bold("║") + yellow(line).ljust(62) + bold("║"))

        print(bold("╠══════════════════════════════════════════════════════════════╣"))
        elapsed_str = f"  Running: {int(elapsed)}s | {self.lines_seen} lines seen | Log: {Path(self.log_path).name}"
        print(bold("║") + dim(elapsed_str).ljust(62) + bold("║"))
        print(bold("╚══════════════════════════════════════════════════════════════╝"))
        print()

    def _once_mode(self):
        """Parse existing log once and show final dashboard."""
        try:
            with open(self.log_path, "r", encoding="utf-8", errors="replace") as f:
                for line in f:
                    self.lines_seen += 1
                    if self._should_keep(line):
                        self._process_line(line)
        except FileNotFoundError:
            print(f"Log file not found: {self.log_path}")
            return
        self._render_dashboard()
        total_passed = sum(s["passed"] for s in self.suites.values())
        total_failed = sum(s["failed"] for s in self.suites.values())
        print(bold(f"Done: {total_passed} passed, {total_failed} failed, "
                   f"{len(self.suites)} suites"))

    def run(self):
        """Main loop: tail the log and process new lines."""
        print(cyan("Rimconemy Live Monitor starting..."))
        print(dim(f"Watching: {self.log_path}"))
        print(dim(f"Level: {self.level} (1=compact, 2=verbose, 3=debug)"))
        print(dim("Ctrl+C to quit"))
        print()

        # Position already set: 0 for --cold (process all), end-of-file for live tail
        if self.position == 0:
            print(dim("(Cold start: processing existing log content...)"))
        else:
            try:
                with open(self.log_path, "r", encoding="utf-8", errors="replace") as f:
                    f.seek(0, 2)  # live mode: start at end
                    self.position = f.tell()
            except FileNotFoundError:
                self.position = 0

        last_size = 0
        try:
            while True:
                try:
                    current_size = os.path.getsize(self.log_path) if os.path.exists(self.log_path) else 0

                    # Detect log rotation (file got smaller → new log)
                    if current_size < self.position:
                        print(yellow(f"[Log rotated at {datetime.now().strftime('%H:%M:%S')}]"))
                        self.position = 0

                    # Read new content
                    if current_size > self.position:
                        with open(self.log_path, "r", encoding="utf-8", errors="replace") as f:
                            f.seek(self.position)
                            new_data = f.read()
                            self.position = f.tell()

                        for line in new_data.splitlines():
                            self.lines_seen += 1
                            if self._should_keep(line):
                                self._process_line(line)

                    # Refresh dashboard every 2 seconds
                    now = time.time()
                    if now - self.last_dashboard > 2.0 and self.level <= 2:
                        self._render_dashboard()
                        self.last_dashboard = now

                    time.sleep(0.5)

                except FileNotFoundError:
                    time.sleep(2)
                    continue
                except IOError:
                    time.sleep(1)
                    continue

        except KeyboardInterrupt:
            print()
            print(cyan("Live monitor stopped."))
            self._render_dashboard()
            print()
            # Final summary
            total_passed = sum(s["passed"] for s in self.suites.values())
            total_failed = sum(s["failed"] for s in self.suites.values())
            print(bold(f"Session summary: {total_passed} passed, {total_failed} failed, "
                       f"{len(self.suites)} suites, {len(self.errors)} errors, "
                       f"{len(self.warnings)} warnings, {len(self.anomalies)} anomalies"))
            if total_failed > 0:
                sys.exit(1)


def main():
    import argparse
    p = argparse.ArgumentParser(description="Rimconemy Live Player.log Monitor")
    p.add_argument("--log", default=DEFAULT_LOG, help=f"Path to Player.log (default: {DEFAULT_LOG})")
    p.add_argument("--level", type=int, default=1, choices=[1, 2, 3],
                   help="Verbosity: 1=compact dashboard, 2=show warnings, 3=debug all lines")
    p.add_argument("--cold", action="store_true",
                   help="Start from beginning of file (for testing with existing logs)")
    p.add_argument("--once", action="store_true",
                   help="Parse once and exit (no live tailing)")
    args = p.parse_args()

    if not os.path.exists(args.log):
        print(f"Log file not found: {args.log}")
        print("Start RimWorld first, or use --log to specify the path.")
        sys.exit(1)

    monitor = LiveMonitor(args.log, args.level)
    if args.cold or args.once:
        monitor.position = 0  # read from beginning
    if args.once:
        monitor._once_mode()
    else:
        monitor.run()


if __name__ == "__main__":
    main()
