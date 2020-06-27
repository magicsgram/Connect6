dotnet publish -c Release -r linux-arm64

cd Server/bin/Release/netcoreapp3.1/linux-arm64/publish/wwwroot
for f in _framework/_bin/*; do mv "$f" "`echo $f | gsed -e 's/\.dll\b/.bbb/g'`"; done
gsed -i 's/\.dll"/.bbb"/g' _framework/blazor.boot.json
rm _framework/blazor.boot.json.br
rm _framework/blazor.boot.json.gz
cd ../../../../../../..