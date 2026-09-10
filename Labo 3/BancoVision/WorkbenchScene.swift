import RealityKit
import SwiftUI

enum WorkbenchLayout {
    // The PDF's Unity coordinates are preserved inside this anchor. RealityKit
    // looks toward -Z, so the anchor offsets the original +1.2 m Z positions
    // back in front of the wearer.
    static let anchorPosition = SIMD3<Float>(0, -1.45, -2.4)
    static let tableSize = SIMD3<Float>(1.6, 0.05, 0.8)
    static let tablePosition = SIMD3<Float>(0, 0.9, 1.2)
    static let motorSize = SIMD3<Float>(0.5, 0.4, 0.4)
    static let motorPosition = SIMD3<Float>(-0.55, 1.13, 1.2)
    static let wrenchSize = SIMD3<Float>(0.30, 0.035, 0.06)
    static let wrenchPosition = SIMD3<Float>(0.25, 0.945, 1.2)
    static let floorSize = SIMD3<Float>(20, 0.02, 20)

    static var wrenchTransform: Transform {
        Transform(scale: .one, rotation: simd_quatf(), translation: wrenchPosition)
    }
}

@MainActor
enum WorkbenchSceneBuilder {
    struct Result {
        let root: Entity
        let wrench: Entity
    }

    static func makeScene() -> Result {
        let anchor = AnchorEntity(world: WorkbenchLayout.anchorPosition)
        anchor.name = "BancoAnchor"

        let floor = makeStaticBox(
            name: "Piso",
            size: WorkbenchLayout.floorSize,
            position: SIMD3<Float>(0, -0.01, 0),
            material: SimpleMaterial(color: .init(white: 0.16, alpha: 1), roughness: 0.9, isMetallic: false)
        )
        anchor.addChild(floor)

        let bench = Entity()
        bench.name = "Banco"
        anchor.addChild(bench)

        let tabletop = makeStaticBox(
            name: "Cubierta",
            size: WorkbenchLayout.tableSize,
            position: WorkbenchLayout.tablePosition,
            material: SimpleMaterial(color: .init(red: 0.15, green: 0.24, blue: 0.30, alpha: 1), roughness: 0.55, isMetallic: true)
        )
        bench.addChild(tabletop)

        let legMaterial = SimpleMaterial(
            color: .init(red: 0.09, green: 0.12, blue: 0.15, alpha: 1),
            roughness: 0.65,
            isMetallic: true
        )
        for x in [-0.70 as Float, 0.70] {
            for z in [0.90 as Float, 1.50] {
                let leg = makeStaticBox(
                    name: "Pata",
                    size: SIMD3<Float>(0.07, 0.88, 0.07),
                    position: SIMD3<Float>(x, 0.44, z),
                    material: legMaterial
                )
                bench.addChild(leg)
            }
        }

        let motor = makeMotor()
        bench.addChild(motor)

        let wrench = makeWrench()
        bench.addChild(wrench)

        return Result(root: anchor, wrench: wrench)
    }

    private static func makeMotor() -> Entity {
        let motorMaterial = SimpleMaterial(
            color: .init(red: 0.13, green: 0.43, blue: 0.52, alpha: 1),
            roughness: 0.35,
            isMetallic: true
        )
        let motor = makeStaticBox(
            name: "Motor",
            size: WorkbenchLayout.motorSize,
            position: WorkbenchLayout.motorPosition,
            material: motorMaterial,
            cornerRadius: 0.035
        )

        let capMaterial = SimpleMaterial(
            color: .init(red: 0.06, green: 0.09, blue: 0.12, alpha: 1),
            roughness: 0.5,
            isMetallic: true
        )
        let cap = ModelEntity(
            mesh: .generateCylinder(height: 0.08, radius: 0.13),
            materials: [capMaterial]
        )
        cap.name = "EjeMotor"
        cap.position = SIMD3<Float>(0.29, 0, 0)
        cap.orientation = simd_quatf(angle: .pi / 2, axis: SIMD3<Float>(0, 0, 1))
        motor.addChild(cap)
        return motor
    }

    private static func makeWrench() -> Entity {
        let wrench = Entity()
        wrench.name = "Torquimetro"
        wrench.transform = WorkbenchLayout.wrenchTransform

        let metal = SimpleMaterial(
            color: .init(red: 0.92, green: 0.55, blue: 0.12, alpha: 1),
            roughness: 0.24,
            isMetallic: true
        )
        let darkMetal = SimpleMaterial(
            color: .init(red: 0.20, green: 0.23, blue: 0.25, alpha: 1),
            roughness: 0.3,
            isMetallic: true
        )

        let handle = ModelEntity(
            mesh: .generateBox(size: SIMD3<Float>(0.23, 0.028, 0.036), cornerRadius: 0.012),
            materials: [metal]
        )
        handle.position = SIMD3<Float>(0.025, 0, 0)
        wrench.addChild(handle)

        let head = ModelEntity(
            mesh: .generateBox(size: SIMD3<Float>(0.075, 0.045, 0.06), cornerRadius: 0.012),
            materials: [darkMetal]
        )
        head.position = SIMD3<Float>(-0.115, 0, 0)
        wrench.addChild(head)

        let endCap = ModelEntity(
            mesh: .generateCylinder(height: 0.014, radius: 0.025),
            materials: [darkMetal]
        )
        endCap.position = SIMD3<Float>(0.147, 0, 0)
        endCap.orientation = simd_quatf(angle: .pi / 2, axis: SIMD3<Float>(0, 0, 1))
        wrench.addChild(endCap)

        let shape = ShapeResource.generateBox(size: WorkbenchLayout.wrenchSize)
        let physicsMaterial = PhysicsMaterialResource.generate(
            staticFriction: 0.72,
            dynamicFriction: 0.62,
            restitution: 0.08
        )
        wrench.components.set(
            PhysicsBodyComponent(
                shapes: [shape],
                mass: 0.45,
                material: physicsMaterial,
                mode: .dynamic
            )
        )
        wrench.components.set(PhysicsMotionComponent())

        ManipulationComponent.configureEntity(
            wrench,
            allowedInputTypes: .all,
            collisionShapes: [shape]
        )

        var manipulation = ManipulationComponent()
        manipulation.dynamics.translationBehavior = .unconstrained
        manipulation.dynamics.primaryRotationBehavior = .unconstrained
        manipulation.dynamics.secondaryRotationBehavior = .unconstrained
        manipulation.dynamics.scalingBehavior = .none
        manipulation.dynamics.inertia = .low
        manipulation.releaseBehavior = .stay
        manipulation.audioConfiguration = .default
        wrench.components.set(manipulation)

        return wrench
    }

    private static func makeStaticBox(
        name: String,
        size: SIMD3<Float>,
        position: SIMD3<Float>,
        material: SimpleMaterial,
        cornerRadius: Float = 0.01
    ) -> ModelEntity {
        let entity = ModelEntity(
            mesh: .generateBox(size: size, cornerRadius: cornerRadius),
            materials: [material]
        )
        entity.name = name
        entity.position = position

        let shape = ShapeResource.generateBox(size: size)
        entity.components.set(CollisionComponent(shapes: [shape]))
        entity.components.set(
            PhysicsBodyComponent(shapes: [shape], mass: 1, mode: .static)
        )
        return entity
    }
}
