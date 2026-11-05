param([string]$Tcc, [string]$WeixinDll)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
New-Item -ItemType Directory -Force $dist | Out-Null
$files = (Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' -File).FullName
& $csc /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /main:Program "/out:$dist/WeixinThemeStudio.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Management.dll /r:System.Xml.dll $files
if ($LASTEXITCODE -ne 0) { throw "C# compilation failed ($LASTEXITCODE)." }

if ($Tcc) {
    $runtime = Join-Path $dist 'native-runtime'
    New-Item -ItemType Directory -Force $runtime | Out-Null
    $module = Join-Path $runtime 'qttestability.dll'
    & $Tcc -m64 -shared -o $module "$root/native/runtime.c" "$root/native/qttestability.def" -luser32 -lkernel32
    if ($LASTEXITCODE -ne 0) { throw "Native compilation failed ($LASTEXITCODE)." }
    Copy-Item "$root/native/qttestability.def" $runtime -Force
    (Get-FileHash $module).Hash.ToLowerInvariant() | Set-Content "$runtime/module.sha256" -Encoding ascii
}
if ($WeixinDll) {
    & node "$root/tools/extract-ui-svg.mjs" $WeixinDll
    if ($LASTEXITCODE -ne 0) { throw "SVG extraction failed ($LASTEXITCODE)." }
}
