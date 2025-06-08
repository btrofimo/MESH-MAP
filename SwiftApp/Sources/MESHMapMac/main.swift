import Foundation

struct Options {
    var date: String
    var time: String = "000000"
    var product: String = "MESH_00.50"
}

func parseArguments() -> Options? {
    var opts = Options(date: "")
    var iterator = CommandLine.arguments.dropFirst().makeIterator()
    while let arg = iterator.next() {
        switch arg {
        case "--date":
            if let value = iterator.next() { opts.date = value }
        case "--time":
            if let value = iterator.next() { opts.time = value }
        case "--product":
            if let value = iterator.next() { opts.product = value }
        default:
            break
        }
    }
    return opts.date.isEmpty ? nil : opts
}

func runPython(script: String, args: [String]) {
    let process = Process()
    process.executableURL = URL(fileURLWithPath: "/usr/bin/env")
    process.arguments = ["python3", script] + args
    process.standardInput = nil
    process.standardOutput = FileHandle.standardOutput
    process.standardError = FileHandle.standardError
    do {
        try process.run()
        process.waitUntilExit()
    } catch {
        print("Failed to run \(script): \(error)")
    }
}

if let opts = parseArguments() {
    runPython(script: "../python/download_mesh.py", args: [opts.date, "--time", opts.time, "--product", opts.product])
    runPython(script: "../python/main.py", args: [])
} else {
    print("Usage: MESHMapMac --date YYYYMMDD [--time HHMMSS] [--product PRODUCT]")
}
