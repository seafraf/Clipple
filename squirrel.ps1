param(
    [Parameter(Position=0, mandatory=$true)]
    [string]$version
)

$githubURL = "https://github.com/seafraf/Clipple"

Set-Alias Squirrel ($env:USERPROFILE + "\.nuget\packages\clowd.squirrel\2.9.42\tools\Squirrel.exe");

# Download relevant files from GitHub releases to form accurate delta packages
Squirrel github-down --repoUrl $githubURL

# Package new version
Squirrel pack --framework "net7.0" --packId "Clipple" --packVersion $version --packDirectory "Clipple.Wpf\bin\x64\Release\net7.0-windows\win-x64"