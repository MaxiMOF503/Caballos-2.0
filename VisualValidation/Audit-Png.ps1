$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class HorsePngAudit {
 public static string Inspect(string path) {
  using (var source=new Bitmap(path)) using(var image=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)) {
   using(var graphics=Graphics.FromImage(image)) graphics.DrawImageUnscaled(source,0,0);
   var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   byte[] bytes=new byte[Math.Abs(data.Stride)*image.Height]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length); image.UnlockBits(data);
   int left=image.Width,top=image.Height,right=-1,bottom=-1,transparent=0,partial=0,edge=0;
   for(int y=0;y<image.Height;y++) for(int x=0;x<image.Width;x++) {
    int alpha=bytes[y*Math.Abs(data.Stride)+x*4+3];
    if(alpha==0){transparent++;continue;} if(alpha<255)partial++;
    left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);
    if(x==0||y==0||x==image.Width-1||y==image.Height-1)edge++;
   }
   return image.Width+","+image.Height+","+transparent+","+partial+","+left+","+top+","+right+","+bottom+","+edge;
  }
 }
}
'@
$projectRoot = Split-Path $PSScriptRoot -Parent
$rows = @('path,width,height,transparentPixels,partialAlphaPixels,left,top,right,bottom,nontransparentEdgePixels')
foreach ($png in Get-ChildItem (Join-Path $projectRoot 'Assets/Art/HorsesRecovered') -Filter '*.png' -Recurse) {
    $rows += $png.FullName.Substring($projectRoot.Length + 1) + ',' + [HorsePngAudit]::Inspect($png.FullName)
}
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'png-audit.csv'), $rows)
Write-Output "Audited PNG files: $($rows.Count - 1)"
