using System;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
static class Program
{
 static readonly OpCode[] One = new OpCode[0x100]; static readonly OpCode[] Two = new OpCode[0x100];
 static Program(){foreach(var f in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)){if(f.GetValue(null) is OpCode o){var v=(ushort)o.Value;if(v<0x100)One[v]=o;else if((v&0xff00)==0xfe00)Two[v&0xff]=o;}}}
 static void Main(){var asm=typeof(Scribe).Assembly; foreach(var spec in new[]{("Verse.ScribeSaver","InitSaving"),("Verse.ScribeSaver","FinalizeSaving"),("Verse.ScribeLoader","InitLoading"),("Verse.ScribeLoader","EnterNode"),("Verse.Scribe","Finalize")}){var t=asm.GetType(spec.Item1);var m=t.GetMethod(spec.Item2,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);Console.WriteLine($"METHOD {m}"); if(m==null)continue; var b=m.GetMethodBody()?.GetILAsByteArray(); if(b==null)continue; var r=m.Module; int i=0; while(i<b.Length){int pos=i; OpCode op; byte x=b[i++];if(x==0xfe)op=Two[b[i++]];else op=One[x]; object arg="";int size=0;switch(op.OperandType){case OperandType.InlineMethod: arg=r.ResolveMethod(BitConverter.ToInt32(b,i));size=4;break;case OperandType.InlineField:arg=r.ResolveField(BitConverter.ToInt32(b,i));size=4;break;case OperandType.InlineType:arg=r.ResolveType(BitConverter.ToInt32(b,i));size=4;break;case OperandType.InlineString:arg=r.ResolveString(BitConverter.ToInt32(b,i));size=4;break;case OperandType.ShortInlineI:arg=b[i];size=1;break;case OperandType.InlineI:arg=BitConverter.ToInt32(b,i);size=4;break;case OperandType.ShortInlineBrTarget:size=1;arg=(sbyte)b[i]+i+1;break;case OperandType.InlineBrTarget:size=4;arg=BitConverter.ToInt32(b,i)+i+4;break;case OperandType.InlineNone:break;default: size=op.OperandType==OperandType.ShortInlineR?4:op.OperandType==OperandType.InlineI8||op.OperandType==OperandType.InlineR?8:op.OperandType==OperandType.InlineSwitch?4:0;break;} Console.WriteLine($" {pos:X4}: {op.Name} {arg}");i+=size;}}
 }
}
