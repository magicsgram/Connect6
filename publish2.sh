dotnet publish -c Release -p:PublishTrimmed=true -r linux-x64

cd Server/bin/Release/netcoreapp3.1/linux-x64/publish/wwwroot
for f in _framework/_bin/*; do mv "$f" "`echo $f | gsed -e 's/\.dll\b/.bbb/g'`"; done
gsed -i 's/\.dll"/.bbb"/g' _framework/blazor.boot.json
rm _framework/blazor.boot.json.br
rm _framework/blazor.boot.json.gz
cd ../../../../../../..
tar -zcvf conn.tar.gz Server/bin/Release/netcoreapp3.1/linux-x64/publish/