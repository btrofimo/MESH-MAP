import SwiftUI

@main
struct MRMSDownloaderApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}

struct ContentView: View {
    @State private var selectedDate = Date()
    @State private var outputFilename: String = ""
    @State private var statusMessage: String? = nil

    private let timestampFormatter: DateFormatter = {
        let df = DateFormatter()
        df.dateFormat = "yyyyMMdd-HHmmss"
        df.timeZone = TimeZone(secondsFromGMT: 0)
        return df
    }()

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Select Date and Time (UTC):")
            DatePicker("", selection: $selectedDate, displayedComponents: [.date, .hourAndMinute])
                .datePickerStyle(.fields)
                .labelsHidden()
                .onChange(of: selectedDate) { _ in
                    updateDefaultFilename()
                }

            Text("Output Filename:")
            TextField("", text: $outputFilename)
                .textFieldStyle(.roundedBorder)
                .disableAutocorrection(true)

            Button("Download GRIB2 File") {
                startDownload()
            }
            .padding(.vertical, 5)

            if let message = statusMessage {
                Text(message)
                    .foregroundColor(message.starts(with: "Error") ? .red : .green)
            }
        }
        .padding(20)
        .frame(width: 400)
        .onAppear {
            updateDefaultFilename()
        }
    }

    private func updateDefaultFilename() {
        let ts = timestampFormatter.string(from: selectedDate)
        outputFilename = "MRMS_MESH_Max_1440min_00.50_\(ts).grib2"
    }

    private func startDownload() {
        statusMessage = "Downloading..."
        let ts = timestampFormatter.string(from: selectedDate)
        let remoteFile = "MRMS_MESH_Max_1440min_00.50_\(ts).grib2.gz"
        let urlString = "https://noaa-mrms-pds.s3.amazonaws.com/CONUS/MESH_Max_1440min_00.50/\(remoteFile)"

        guard let url = URL(string: urlString) else {
            statusMessage = "Error: Malformed URL."
            return
        }

        let task = URLSession.shared.dataTask(with: url) { data, response, error in
            if let error = error {
                DispatchQueue.main.async {
                    statusMessage = "Error: \(error.localizedDescription)"
                }
                return
            }

            if let http = response as? HTTPURLResponse {
                if http.statusCode == 404 {
                    DispatchQueue.main.async {
                        statusMessage = "Error: File not found on server (check date/time)."
                    }
                    return
                } else if http.statusCode != 200 {
                    DispatchQueue.main.async {
                        statusMessage = "Error: Server returned status \(http.statusCode)."
                    }
                    return
                }
            }

            guard let fileData = data else {
                DispatchQueue.main.async {
                    statusMessage = "Error: No data received (unknown issue)."
                }
                return
            }

            let desktop = FileManager.default.urls(for: .desktopDirectory, in: .userDomainMask).first!
            var destURL = desktop.appendingPathComponent(outputFilename)
            if destURL.pathExtension.isEmpty {
                destURL.appendPathExtension("grib2")
            }

            do {
                try fileData.write(to: destURL)
                DispatchQueue.main.async {
                    statusMessage = "Download complete: \(destURL.lastPathComponent) saved to Desktop."
                }
            } catch {
                DispatchQueue.main.async {
                    statusMessage = "Error: Failed to save file (\(error.localizedDescription))."
                }
            }
        }
        task.resume()
    }
}

