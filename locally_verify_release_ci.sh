cd BuildCI

for line in $(dotnet run --configuration Release Program.cs); do
  IFS='|' read -ra parts <<< "$line"
  namespace=${parts[0]}
  name_underscore_separator=${parts[1]}
  version=${parts[2]}
  csproj_folder=${parts[3]}
  nuget_package_name=${parts[4]}

  cd $csproj_folder

  is_project_built=false

  existing_version_number=$(curl --compressed -s "https://api-v2v3search-0.nuget.org/query?q=packageid:${nuget_package_name}&prerelease=true&semVerLevel=2.0.0" | jq '.data[0]?.versions[]?.version' | grep "${version}\"" || true)
  if [ -z $existing_version_number ]
  then
    dotnet pack --configuration Release /p:PackageOutputPath=./ReleaseOutput /p:OutputPath=./ReleaseOutput
    is_project_built=true

    if [ "$namespace" == "RiskofThunder" ]; then
      #nuget setapikey "${{ secrets.NUGET_API_KEY_RISK_OF_THUNDER }}"
      echo "setapikey"
    else
      #nuget setapikey "${{ secrets.NUGET_API_KEY }}"
      echo "setapikey"
    fi
    
    #nuget push ./ReleaseOutput/*.nupkg -Source 'https://api.nuget.org/v3/index.json'
    echo "nuget push 1"
    #find . -name '*.nupkg' -type f -delete
  fi

  existing_version_number=$(curl --compressed -s "https://thunderstore.io/api/v1/package/" | jq --arg package_name "$namespace-$name_underscore_separator" '.[]? | select(.full_name|startswith($package_name)) | .versions[0]?.version_number' | grep "${version}\"" || true)
  if [ -z $existing_version_number ]
  then
    if [ "$is_project_built" = false ] ; then
      dotnet pack --configuration Release /p:PackageOutputPath=./ReleaseOutput /p:OutputPath=./ReleaseOutput
      is_project_built=true
    fi

    find . -name '*.pdb' -type f -delete
    find . -name '*.deps.json' -type f -delete

    if [ "$namespace" == "RiskofThunder" ]; then
      #tcli publish --token ${{ secrets.TCLI_AUTH_TOKEN_RISK_OF_THUNDER }}
      echo "tcli publish 1"
    else
      #tcli publish --token ${{ secrets.TCLI_AUTH_TOKEN }}
      echo "tcli publish 2"
    fi

    #rm -rf ./build
    echo "rm -rf ./build"
  fi

  if [ "$is_project_built" = true ] ; then
    #rm -rf ./ReleaseOutput
    echo "rm -rf ./ReleaseOutput"
  fi
done
