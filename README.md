# Adeptik NodeJs Redistributable

MSBuild NodeJs installation target.

![Build Status](https://tfs.adeptik.com/Adeptik/_apis/public/build/definitions/5f6da651-409b-4516-b0c6-16518d60e6e9/137/badge)
![Nuget Package](https://img.shields.io/nuget/vpre/Adeptik.NodeJs.Redistributable)

## Usage

To aquire required version of NodeJs follow steps:

1. Install Adeptik.NodeJs.Redistributable package from Nuget

        Install-Package Adeptik.NodeJs.Redistributable -Version 1.0.0

    or add in your .csproj:

        <PackageReference Include="Adeptik.NodeJs.Redistributable" Version="1.0.0" />

2. Specify a version of NodeJs youwish to use by adding property in .csproj:

    ```xml
    <PropertyGroup>
        <NodeJsDistVersion>12.15.0</NodeJsDistVersion>
    </PropertyGroup>
    ```

3. Now you can get full path to the downloaded distribution in your target by setting target's `DependsOn` attribute to `InstallNodeJs` and reading `$(NodeJsExecutablePath)` property:

    ```xml
    <Target Name="YourTarget" DependsOnTargets="InstallNodeJs">
        <Message Text="Using $(NodeJsExecutablePath)" />
    </Target>
    ```
