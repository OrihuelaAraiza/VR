import Combine
import Metal
import Observation
import RealityKit

@MainActor
@Observable
final class ExperienceModel {
    static let immersiveSpaceID = "BancoImmersiveSpace"

    enum ImmersiveState {
        case closed
        case transitioning
        case open
    }

    var immersiveState: ImmersiveState = .closed
    var statusMessage = "Listo para abrir el banco."
    var lastHand: String?

    @ObservationIgnored
    private var subscriptions: [any Cancellable] = []

    @ObservationIgnored
    private weak var wrench: Entity?

    var isImmersiveOpen: Bool { immersiveState == .open }
    var isTransitioning: Bool { immersiveState == .transitioning }

    var primaryActionTitle: String {
        if isTransitioning { return "Procesando…" }
        return isImmersiveOpen ? "Cerrar banco" : "Abrir banco"
    }

    func connect(to wrench: Entity) {
        self.wrench = wrench
        subscriptions.removeAll()

        guard let scene = wrench.scene else {
            statusMessage = "La escena no quedó conectada."
            return
        }

        subscriptions.append(
            scene.subscribe(to: ManipulationEvents.WillBegin.self, on: wrench) { [weak self] event in
                let devices = event.inputDeviceSet
                Task { @MainActor [weak self] in
                    self?.manipulationBegan(with: devices)
                }
            }
        )

        subscriptions.append(
            scene.subscribe(to: ManipulationEvents.DidHandOff.self, on: wrench) { [weak self] event in
                let devices = event.newInputDeviceSet
                Task { @MainActor [weak self] in
                    self?.manipulationHandedOff(to: devices)
                }
            }
        )

        subscriptions.append(
            scene.subscribe(to: ManipulationEvents.WillRelease.self, on: wrench) { [weak self] event in
                let cancelled = event.wasCancelled
                Task { @MainActor [weak self] in
                    self?.manipulationReleased(cancelled: cancelled)
                }
            }
        )

        immersiveState = .open
        statusMessage = "Banco abierto. Acerca la mano y pellizca el torquímetro."
        logDeviceReport()
    }

    func resetWrench() {
        guard let wrench else { return }

        wrench.transform = WorkbenchLayout.wrenchTransform
        wrench.components.set(PhysicsMotionComponent())
        statusMessage = "Torquímetro recolocado sobre la mesa."
        print("torquímetro recolocado")
    }

    private func manipulationBegan(with devices: ManipulationEvents.InputDeviceSet) {
        let hand = Self.handDescription(for: devices)
        lastHand = hand
        statusMessage = "Torquímetro en \(hand)."
        print("torquímetro en \(hand)")
    }

    private func manipulationHandedOff(to devices: ManipulationEvents.InputDeviceSet) {
        let hand = Self.handDescription(for: devices)
        lastHand = hand
        statusMessage = "Torquímetro transferido a \(hand)."
        print("torquímetro transferido a \(hand)")
    }

    private func manipulationReleased(cancelled: Bool) {
        statusMessage = cancelled
            ? "Agarre cancelado; la física continúa."
            : "Torquímetro suelto; gravedad activa."
        print(cancelled ? "agarre cancelado" : "torquímetro suelto")
    }

    static func handDescription(for devices: ManipulationEvents.InputDeviceSet) -> String {
        let chiralities = Set(devices.compactMap(\.chirality))

        if chiralities.contains(.left), chiralities.contains(.right) {
            return "ambas manos"
        }
        if chiralities.contains(.left) {
            return "mano izquierda"
        }
        if chiralities.contains(.right) {
            return "mano derecha"
        }
        return "entrada sin lateralidad"
    }

    private func logDeviceReport() {
        #if targetEnvironment(simulator)
        let simulator = true
        #else
        let simulator = false
        #endif

        print("os    \(ProcessInfo.processInfo.operatingSystemVersionString)")
        print("gfx   \(MTLCreateSystemDefaultDevice()?.name ?? "sin Metal")")
        print("sim   \(simulator)")
    }
}
