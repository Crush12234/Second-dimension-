param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [string]$Icon=(Join-Path $PSScriptRoot 'SECOND_DIMENSION.ico'),
 [Parameter(Mandatory=$true)][string]$OutputExecutable
)
$ErrorActionPreference='Stop'
$taskInput=[IO.Path]::GetFullPath($Executable)
$taskOutput=[IO.Path]::GetFullPath($OutputExecutable)
$taskIcon=[IO.Path]::GetFullPath($Icon)
if($taskInput -eq $taskOutput -or (Test-Path -LiteralPath $taskOutput)){throw 'Use a new output EXE; the input is preserved.'}
if((Get-AuthenticodeSignature -LiteralPath $taskInput).Status -eq 'Valid'){throw 'Apply branding before signing the executable.'}
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
public static class SecondDimensionIcon165 {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibraryExW(string path,IntPtr file,uint flags);
 [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
 delegate bool EnumType(IntPtr module,IntPtr type,IntPtr data);
 delegate bool EnumName(IntPtr module,IntPtr type,IntPtr name,IntPtr data);
 delegate bool EnumLang(IntPtr module,IntPtr type,IntPtr name,ushort lang,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool EnumResourceTypesW(IntPtr module,EnumType cb,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool EnumResourceNamesW(IntPtr module,IntPtr type,EnumName cb,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool EnumResourceLanguagesW(IntPtr module,IntPtr type,IntPtr name,EnumLang cb,IntPtr data);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr FindResourceExW(IntPtr module,IntPtr type,IntPtr name,ushort lang);
 [DllImport("kernel32.dll",SetLastError=true)] static extern uint SizeofResource(IntPtr module,IntPtr resource);
 [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr LoadResource(IntPtr module,IntPtr resource);
 [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr BeginUpdateResourceW(string path,bool deleteExisting);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool UpdateResourceW(IntPtr update,IntPtr type,IntPtr name,ushort lang,byte[] bytes,uint count);
 [DllImport("kernel32.dll",SetLastError=true)] static extern bool EndUpdateResourceW(IntPtr update,bool discard);
 class Res { public string Type,Name; public ushort Lang; public byte[] Bytes; public string Key {get{return Type+"|"+Name+"|"+Lang;}} }
 static string Id(IntPtr p) { return p.ToInt64()<=65535 ? "#"+p.ToInt64() : Marshal.PtrToStringUni(p); }
 static IntPtr Ptr(string id,out bool allocated) {allocated=!id.StartsWith("#");return allocated?Marshal.StringToHGlobalUni(id):new IntPtr(int.Parse(id.Substring(1)));}
 static void Check(bool ok) {if(!ok)throw new Win32Exception(Marshal.GetLastWin32Error());}
 static List<Res> Read(string path) {
  var result=new List<Res>();var module=LoadLibraryExW(path,IntPtr.Zero,2);if(module==IntPtr.Zero)throw new Win32Exception();
  try { Check(EnumResourceTypesW(module,delegate(IntPtr m,IntPtr t,IntPtr d){
   Check(EnumResourceNamesW(m,t,delegate(IntPtr m2,IntPtr t2,IntPtr n,IntPtr d2){
    Check(EnumResourceLanguagesW(m2,t2,n,delegate(IntPtr m3,IntPtr t3,IntPtr n3,ushort l,IntPtr d3){
     var resource=FindResourceExW(m3,t3,n3,l);var bytes=new byte[SizeofResource(m3,resource)];
     Marshal.Copy(LockResource(LoadResource(m3,resource)),bytes,0,bytes.Length);
     result.Add(new Res{Type=Id(t3),Name=Id(n3),Lang=l,Bytes=bytes});return true;
    },IntPtr.Zero));return true;
   },IntPtr.Zero));return true;
  },IntPtr.Zero)); } finally {FreeLibrary(module);} return result;
 }
 static void Put(IntPtr update,Res resource) {
  bool ta,na;var t=Ptr(resource.Type,out ta);var n=Ptr(resource.Name,out na);
  try {Check(UpdateResourceW(update,t,n,resource.Lang,resource.Bytes,resource.Bytes==null?0:(uint)resource.Bytes.Length));}
  finally {if(ta)Marshal.FreeHGlobal(t);if(na)Marshal.FreeHGlobal(n);}
 }
 static string Hash(byte[] bytes){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes));}
 static Dictionary<string,string> CodeSections(string path) {
  var result=new Dictionary<string,string>();using(var r=new BinaryReader(File.OpenRead(path))){
   r.BaseStream.Position=60;int pe=r.ReadInt32();r.BaseStream.Position=pe;if(r.ReadUInt32()!=0x4550)throw new Exception("Not a PE executable");
   ushort machine=r.ReadUInt16(),count=r.ReadUInt16();r.BaseStream.Position=pe+20;ushort optional=r.ReadUInt16();r.BaseStream.Position=pe+24+optional;
   for(int i=0;i<count;i++){long next=r.BaseStream.Position+40;string name=System.Text.Encoding.ASCII.GetString(r.ReadBytes(8)).TrimEnd('\0');
    r.ReadUInt32();r.ReadUInt32();uint size=r.ReadUInt32(),offset=r.ReadUInt32();r.BaseStream.Position=offset;
    if(name!=".rsrc")result.Add(name,Hash(r.ReadBytes((int)size)));r.BaseStream.Position=next;
   }result.Add("machine",machine.ToString());
  }return result;
 }
 public static string Apply(string input,string icon,string output) {
  var before=Read(input);var groups=before.FindAll(r=>r.Type=="#14");if(groups.Count==0)throw new Exception("No existing app icon group");
  var entries=new List<byte[]>();var images=new List<byte[]>();var data=File.ReadAllBytes(icon);
  using(var r=new BinaryReader(new MemoryStream(data))){if(r.ReadUInt16()!=0||r.ReadUInt16()!=1)throw new Exception("Invalid ICO");int count=r.ReadUInt16();if(count<1||count>32)throw new Exception("Invalid ICO count");
   for(int i=0;i<count;i++){r.BaseStream.Position=6+16*i;var entry=r.ReadBytes(12);uint size=BitConverter.ToUInt32(entry,8),offset=r.ReadUInt32();if(size==0||offset+size>data.Length)throw new Exception("Invalid ICO image");
    r.BaseStream.Position=offset;entries.Add(entry);images.Add(r.ReadBytes((int)size));}
  }
  File.Copy(input,output,false);var update=BeginUpdateResourceW(output,false);if(update==IntPtr.Zero)throw new Win32Exception();bool committed=false;
  try {
   foreach(var res in before)if(res.Type=="#3")Put(update,new Res{Type=res.Type,Name=res.Name,Lang=res.Lang,Bytes=null});
   foreach(var group in groups){using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
    w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)entries.Count);
    for(int i=0;i<entries.Count;i++){w.Write(entries[i]);w.Write((ushort)(i+1));Put(update,new Res{Type="#3",Name="#"+(i+1),Lang=group.Lang,Bytes=images[i]});}
    Put(update,new Res{Type="#14",Name=group.Name,Lang=group.Lang,Bytes=stream.ToArray()});
   }}
   Check(EndUpdateResourceW(update,false));committed=true;
  }finally{if(!committed)EndUpdateResourceW(update,true);}
  var after=Read(output);var other=new Dictionary<string,string>();foreach(var res in before)if(res.Type!="#3"&&res.Type!="#14")other.Add(res.Key,Hash(res.Bytes));
  foreach(var res in after)if(res.Type!="#3"&&res.Type!="#14"){if(!other.ContainsKey(res.Key)||other[res.Key]!=Hash(res.Bytes))throw new Exception("Non-icon resource changed");other.Remove(res.Key);}
  if(other.Count!=0)throw new Exception("Non-icon resource lost");
  var first=CodeSections(input);var second=CodeSections(output);if(first.Count!=second.Count)throw new Exception("PE section inventory changed");foreach(var pair in first)if(!second.ContainsKey(pair.Key)||second[pair.Key]!=pair.Value)throw new Exception("Executable code changed: "+pair.Key);
  foreach(var group in after.FindAll(r=>r.Type=="#14"))using(var r=new BinaryReader(new MemoryStream(group.Bytes))){
   r.ReadUInt32();if(r.ReadUInt16()!=images.Count)throw new Exception("Icon size inventory differs");
   for(int i=0;i<images.Count;i++){r.ReadBytes(12);string id="#"+r.ReadUInt16();var resource=after.Find(v=>v.Type=="#3"&&v.Name==id&&v.Lang==group.Lang);if(resource==null||Hash(resource.Bytes)!=Hash(images[i]))throw new Exception("Embedded icon differs");}
  }
  return "Verified "+groups.Count+" app icon groups, "+images.Count+" sizes; executable code and non-icon resources unchanged.";
 }
}
'@
[SecondDimensionIcon165]::Apply($taskInput,$taskIcon,$taskOutput)
Get-FileHash -LiteralPath $taskOutput -Algorithm SHA256 | Select-Object Path,Hash
