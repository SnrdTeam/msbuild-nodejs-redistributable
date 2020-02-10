# Adeptik NodeJs Redistributable

MSBuild NodeJs installation target.

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
