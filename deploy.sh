echo "Pulling the remote Git repo..."
git pull 

echo "Building the source..."
dotnet publish -c Release -o bin/Release/net7.0/

echo "Starting ..."
dotnet run --configuration Release --urls="http://0.0.0.0:5000;https://0.0.0.0:5001" &

