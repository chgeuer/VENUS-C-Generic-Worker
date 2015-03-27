Get-ChildItem * -include bin  -Recurse | Remove-Item -Recurse -Force
Get-ChildItem * -include obj  -Recurse | Remove-Item -Recurse -Force
Get-ChildItem * -include *.js  -Recurse | Remove-Item -Recurse -Force
Get-ChildItem * -include *jquery*  -Recurse | Remove-Item -Recurse -Force
Get-ChildItem * -include *.dll  -Recurse | Remove-Item -Force
Get-ChildItem * -include *.exe  -Recurse | Remove-Item -Force
Get-ChildItem * -include *.zip  -Recurse | Remove-Item -Force
Get-ChildItem * -include *.xml  -Recurse | Remove-Item -Force
Get-ChildItem * -include *.pdb  -Recurse | Remove-Item -Force
Get-ChildItem packages -exclude reposi* | Remove-Item -Recurse -Force
Get-ChildItem libs -exclude *.txt -Recurse | Remove-Item -Recurse -Force