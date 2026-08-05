
import os
import time

log_path = os.path.expanduser('~/.steam/steam/steamapps/compatdata/294100/pfx/drive_c/users/steamuser/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Player.log')

def watch_log():
    with open(log_path, 'r') as log_file:
        while True:
            line = log_file.readline()
            if 'Error' in line or 'Exception' in line:
                with open('/home/vannon/Schreibtisch/Rimconemy/qa_findings.md', 'a') as qa_file:
                    qa_file.write(line + '\n')
            time.sleep(1)

watch_log()
