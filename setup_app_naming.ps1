$Find = "Appname"
$Replace = $(Get-Item .).Name
$Path = "./"


write-host "Renaming files"

$TargetFiles =  Get-ChildItem $Path -Recurse -file -Depth 100| Where-Object {$_.Name -like "*$Find*"}
ForEach($File in $TargetFiles) 
{
    $newname = ([String]$File).Replace($Find,$Replace)
    write-host "Renaming $File to $newname"
    Rename-item -Path $File.PSPath $newname
}

write-host "Renaming folders"

$TargetFolders =  Get-ChildItem $Path -Recurse -directory -Depth 100| Where-Object {$_.Name -like "*$Find*"}
ForEach($targetFolder in $TargetFolders) 
{
    $newname = ([String]$targetFolder).Replace($Find,$Replace)
    write-host "Renaming $targetFolder to $newname"
    Rename-item -Path $targetFolder.PSPath $newname
}

write-host "Renaming contents of files"

$fileNames = Get-ChildItem $Path -Recurse -file -Depth 100| Where-Object{$_.Name -notlike "*.ps1"} | select -expand fullname

foreach ($filename in $filenames) 
{
  (  Get-Content $fileName) -replace $Find, $Replace | Set-Content $fileName
      write-host "Renaming $Find to $Replace in $filename"
}
