// swift-tools-version:6.1
import PackageDescription

let package = Package(
    name: "MESHMapMac",
    platforms: [
        .macOS(.v12)
    ],
    targets: [
        .executableTarget(
            name: "MESHMapMac",
            dependencies: [],
            resources: [
                .copy("Resources/requirements.txt"),
                .copy("Resources/install_python_deps.sh")
            ]
        )
    ]
)
