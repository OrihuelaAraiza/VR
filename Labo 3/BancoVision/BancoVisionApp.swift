import SwiftUI

@main
struct BancoVisionApp: App {
    @State private var model = ExperienceModel()

    var body: some Scene {
        WindowGroup {
            ContentView(model: model)
        }
        .windowResizability(.contentSize)

        ImmersiveSpace(id: ExperienceModel.immersiveSpaceID) {
            ImmersiveView(model: model)
                .upperLimbVisibility(.visible)
        }
        .immersionStyle(selection: .constant(.mixed), in: .mixed)
    }
}
