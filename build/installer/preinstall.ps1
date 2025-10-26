Param(
  [string]$BuildOutput = "EquipmentTracker\bin\Release",
  [string]$ReleaseDir = "release\EquipmentTracker"
)

Write-Host "Preinstall checks and config enforcement"

# Ensure release dir exists
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# Ensure config exists with SQLite provider
$configPath = Join-Path $BuildOutput 'EquipmentTracker.exe.config'
if (-not (Test-Path $configPath)) {
  Write-Host "Config missing. Creating default config..."
  @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <probing privatePath="lib;plugins" />
    </assemblyBinding>
    <loadFromRemoteSources enabled="true"/>
  </runtime>
  <system.data>
    <DbProviderFactories>
      <add name="SQLite Data Provider" invariant="System.Data.SQLite" description=".NET Framework Data Provider for SQLite" type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
    </DbProviderFactories>
  </system.data>
  <appSettings>
    <add key="DatabasePath" value="equipment.db" />
    <add key="EnableLogging" value="true" />
  </appSettings>
</configuration>
"@ | Set-Content -Path $configPath -Encoding UTF8
}

# Copy output
Copy-Item "$BuildOutput\*" $ReleaseDir -Recurse -Force -ErrorAction SilentlyContinue

# Touch an empty database if not present
$dbPath = Join-Path $ReleaseDir 'equipment.db'
if (-not (Test-Path $dbPath)) { '' | Set-Content $dbPath }

Write-Host "Preinstall complete"