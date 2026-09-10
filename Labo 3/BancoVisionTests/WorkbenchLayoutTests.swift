import XCTest
@testable import BancoVision

final class WorkbenchLayoutTests: XCTestCase {
    func testDimensionsMatchTheLabSpecification() {
        XCTAssertEqual(WorkbenchLayout.tableSize.x, 1.6, accuracy: 0.0001)
        XCTAssertEqual(WorkbenchLayout.tableSize.y, 0.05, accuracy: 0.0001)
        XCTAssertEqual(WorkbenchLayout.tableSize.z, 0.8, accuracy: 0.0001)
        XCTAssertEqual(WorkbenchLayout.tablePosition.y, 0.9, accuracy: 0.0001)
        XCTAssertEqual(WorkbenchLayout.wrenchSize.x, 0.30, accuracy: 0.0001)
    }

    func testWrenchStartsRestingOnTheTable() {
        let tabletop = WorkbenchLayout.tablePosition.y + WorkbenchLayout.tableSize.y / 2
        let wrenchBottom = WorkbenchLayout.wrenchPosition.y - WorkbenchLayout.wrenchSize.y / 2

        XCTAssertEqual(tabletop, wrenchBottom, accuracy: 0.0001)
    }

    func testMotorStartsAboveTheTable() {
        let tabletop = WorkbenchLayout.tablePosition.y + WorkbenchLayout.tableSize.y / 2
        let motorBottom = WorkbenchLayout.motorPosition.y - WorkbenchLayout.motorSize.y / 2

        XCTAssertGreaterThanOrEqual(motorBottom, tabletop)
    }
}
