import SwiftUI

struct ContentView: View {
    @Bindable var model: ExperienceModel

    @Environment(\.dismissImmersiveSpace) private var dismissImmersiveSpace
    @Environment(\.openImmersiveSpace) private var openImmersiveSpace

    var body: some View {
        VStack(alignment: .leading, spacing: 22) {
            HStack(spacing: 14) {
                Image(systemName: "visionpro")
                    .font(.system(size: 36, weight: .medium))
                    .symbolRenderingMode(.hierarchical)

                VStack(alignment: .leading, spacing: 3) {
                    Text("Banco Vision")
                        .font(.largeTitle.bold())
                    Text("Laboratorio 3 · Apple Vision Pro")
                        .foregroundStyle(.secondary)
                }
            }

            Text("Abre el banco inmersivo, toma el torquímetro con un pellizco y muévelo con cualquiera de las dos manos. Al soltarlo, la física vuelve a actuar.")
                .font(.title3)
                .fixedSize(horizontal: false, vertical: true)

            VStack(alignment: .leading, spacing: 11) {
                Label("Mesa a escala real: 1.60 × 0.80 m", systemImage: "ruler")
                Label("Torquímetro de 30 cm", systemImage: "wrench.adjustable")
                Label("Manos naturales, sin controles", systemImage: "hand.raised")
                Label("Gravedad y colisiones al soltar", systemImage: "arrow.down.to.line")
            }
            .font(.headline)

            Divider()

            HStack(spacing: 12) {
                Button {
                    Task { await toggleImmersiveSpace() }
                } label: {
                    Label(model.primaryActionTitle, systemImage: model.isImmersiveOpen ? "xmark" : "arkit")
                        .frame(minWidth: 190)
                }
                .buttonStyle(.borderedProminent)
                .controlSize(.large)
                .disabled(model.isTransitioning)

                Button("Recolocar torquímetro", systemImage: "arrow.counterclockwise") {
                    model.resetWrench()
                }
                .controlSize(.large)
                .disabled(!model.isImmersiveOpen)
            }

            statusPanel
        }
        .padding(30)
        .frame(width: 620)
    }

    private var statusPanel: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Estado")
                .font(.caption.weight(.semibold))
                .foregroundStyle(.secondary)
                .textCase(.uppercase)

            Text(model.statusMessage)
                .font(.body.monospaced())
                .contentTransition(.numericText())

            if let lastHand = model.lastHand {
                Label("Último agarre: \(lastHand)", systemImage: "hand.point.up.left.fill")
                    .foregroundStyle(.cyan)
            }
        }
        .padding(16)
        .frame(maxWidth: .infinity, alignment: .leading)
        .glassBackgroundEffect(in: .rect(cornerRadius: 18))
    }

    @MainActor
    private func toggleImmersiveSpace() async {
        guard !model.isTransitioning else { return }

        if model.isImmersiveOpen {
            model.immersiveState = .transitioning
            await dismissImmersiveSpace()
            model.immersiveState = .closed
            model.statusMessage = "Banco cerrado."
            return
        }

        model.immersiveState = .transitioning
        model.statusMessage = "Abriendo espacio inmersivo…"

        switch await openImmersiveSpace(id: ExperienceModel.immersiveSpaceID) {
        case .opened:
            model.immersiveState = .open
        case .userCancelled:
            model.immersiveState = .closed
            model.statusMessage = "Apertura cancelada."
        case .error:
            model.immersiveState = .closed
            model.statusMessage = "No se pudo abrir el espacio inmersivo."
        @unknown default:
            model.immersiveState = .closed
            model.statusMessage = "Resultado desconocido al abrir el espacio."
        }
    }
}
