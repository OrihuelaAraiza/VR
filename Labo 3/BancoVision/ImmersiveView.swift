import RealityKit
import SwiftUI

struct ImmersiveView: View {
    let model: ExperienceModel

    var body: some View {
        RealityView { content in
            let scene = WorkbenchSceneBuilder.makeScene()
            content.add(scene.root)
            model.connect(to: scene.wrench)
        }
    }
}
