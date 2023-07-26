/**
 * Data definitions for "Low voltage network"
 */
export interface GridSupplyPoint {
    id: number,
    nrId: string,
    nr: string,
    name: string,
    gisData: GISData
    numberOfPrimarySubstations: number
}

export interface PrimarySubstation {
    id: number,
    nrId: string,
    nr: string,
    name: string,
    gisData: GISData,
    numberOfDistributionSubstations: number
}

export interface GISData {
    id: number,
    latitude: number,
    longitude: number,
    //?? Now done via GISBoundary
    //??boundaryLatitudes: number[] | null,
    //??boundaryLongitudes: number[] | null,
}

export interface GISBoundary {
    latitudes: number[] | null;
    longitudes: number[] | null;
}

export interface DistributionSubstation {
    id: number,
    nrId: string,
    nr: string,
    name: string,
    gisData: GISData,
    classifications: SubstationClassification[]
    loadProfiles: SubstationLoadProfile[]
    substationParams: SubstationParams,
    chargingParams: SubstationChargingParams,
    heatingParams: SubstationHeatingParams
}

export interface SubstationParams {
    mount: SubstationMount
    rating: number
    percentIndustrialCustomers: number
    numberOfFeeders: number
    percentageHalfHourlyLoad: number
    totalLength: number
    percentageOverhead: number
}

export interface SubstationChargingParams {
    numHomeChargers: number,
    numType1EVs: number,
    numType2EVs: number,
    numType3EVs: number
}

export interface SubstationHeatingParams {
    numType1HPs: number,
    numType2HPs: number,
    numType3HPs: number
}

export enum Day { Saturday = 0, Sunday, Weekday }

export enum Season { Winter, Spring, Summer, Autumn, NotSet}

export enum LoadProfileSource { LV_Spreadsheet, Tool, EV_Pred, HP_Pred }

export interface SubstationLoadProfile {
    id: number,
    intervalMins: number,
    year: number,
    monthNumber: number,
    season: Season,
    day: Day,
    data: number[],
    deviceCount: number,
    carbon: number[],
    cost: number[],
    source: LoadProfileSource
}

export interface SubstationClassification {
    id: number,
    num: number,
    consumptionKwh: number,
    numberOfCustomers: number,
    numberOfEACs: number,
}

export interface GeographicalArea {
    id: number,
    name: string,
    gisData: GISData,
    numberOfGridSupplyPoints: number
}

export interface NameValuePair {
    name: string,
    value: number,
    disabled?: boolean
}

/**
 * Vehicle charging
 */
export enum VehicleChargingStationSourceEnum {Manual, OpenChargeMap}

export interface VehicleChargingStation {
    id: number,
    externalId: string,
    name: string,
    source: VehicleChargingStationSourceEnum,
    gisData: GISData,
    primarySubstationId: number,
    connections: VehicleChargingConnection[]
}

export interface VehicleChargingConnection {
    id: number,
    externalId: string,
    quantity: number,
    current: number,
    voltage: number,
    powerKw: number,
    connectionType: VehicleChargingConnectionType,
    currentType: VehicleChargingCurrentType
}

export interface VehicleChargingConnectionType
{
    id: number,
    externalId: string,
    name: string,
    formalName: string
}

export interface VehicleChargingCurrentType
{
    id: number,
    externalId: string,
    name: string,
    formalName: string
}



/** 
 * Data definitions for Classification Tool 
 */
export enum SubstationMount { Ground = 0, Pole = 1 }

export interface ClassificationToolInput {
    elexonProfile: number[]
    substationMount: SubstationMount
    transformerRating: number
    percentIndustrialCustomers: number
    numberOfFeeders: number
    percentageHalfHourlyLoad: number
    totalLength: number
    percentageOverhead: number
}

export interface ClassificationToolOutput {
    clusterNumber: number
    clusterProbabilities: number[]
    loadProfile: LoadProfile
}

export interface LoadProfile {
    all: DayLoadProfile
    weekday: DayLoadProfile
    saturday: DayLoadProfile
    sunday: DayLoadProfile
    timeOfDay: string[]
}

export interface DayLoadProfile {
    load: number[]
    peak: number
}

/**
 * Loadflow
 */

export interface LoadflowResults {
    stageResults: StageResults,
    nodes: NodeWrapper[],
    branches: BranchWrapper[],
    ctrls: CtrlWrapper[],
    boundaryFlowResult: BoundaryFlowResult,
    boundaryTrips: BoundaryTrips,
    singleTrips: AllTripResult[],
    doubleTrips: AllTripResult[]
}

export interface Boundary {
    id: number,
    code: string
}

export enum BoundaryTripType { Single = 0, Double = 1}
export interface BoundaryTrip {
    index: number,
    type: BoundaryTripType,
    lineNames: string[],
    text: string
}

export interface BoundaryTrips {
    trips: BoundaryTrip[],
    lineNames: string[]
}

export interface StageResults {
    results: StageResult[]
    nodeResults: NodeResult[],
    branchResults: BranchResult[],
    ctrlResults: CtrlResult[],
    boundaryFlowResult: BoundaryFlowResult,
    boundaryTrips: BoundaryTrips
}

export enum StageResultEnum { Pass=0, Fail=1, Warn=2 }
export interface StageResult {
    name: string,
    result: StageResultEnum,
    comment: string
}

export interface NodeResult {
    id: number,
    code: string,
    misMatch: number | null
}

export interface BranchResult {
    id: number,
    code: string,
    powerFlow: number | null,
    freePower: number | null
}

export interface CtrlResult {
    id: number,
    code: string,
    setPoint: number | null
}

export interface BoundaryFlowResult {
    genInside: number,
    demInside: number,
    genOutside: number,
    demOutside: number,
    ia: number
}

export interface NodeWrapper {
    obj: Node
    mismatch: number | null
}

export interface Node {
    id: number
    code: string
    name: string
    demand: number
    generation: number
    ext: boolean
    zone: Zone
    gisData: GISData | undefined
}

export interface BranchWrapper {
    obj: Branch
    lineName: string
    outaged: boolean
    powerFlow: number | null
    bFlow: number
    freePower: number | null
}

export interface Branch {
    id: number
    region: string
    code: string
    r: number
    x: number
    ohl: number
    cap: number
    linkType: string
    node1Code: string
    node2Code: string
    node1Name: string
    node2Name: string
    node1: Node
    node2: Node
}

export interface CtrlWrapper {
    obj: Ctrl
    branch: BranchWrapper
    injMax: number
    setPoint: number | null
}

export enum LoadflowCtrlType {  QB=0,  // Quad booster
                                HVDC=1 // High-voltage DC 
                             }
export interface Ctrl {
    id: number
    region: string
    code: string
    minCtrl: number
    maxCtrl: number
    cost: number
    type: LoadflowCtrlType
    node1Code: string
    node2Code: string
    node1Name: string
    node2Name: string
    node1: Node
    node2: Node
}

export interface Zone {
    id: number
    code: string
}

export interface NetworkData {
    nodes: NodeWrapper[]
    branches: BranchWrapper[]
    ctrls: CtrlWrapper[]
}

export interface LocationData {
    locations: LoadflowLocation[]
    branches: LoadflowBranch[]
}

export interface LoadflowLocation {
    id: number
    name: string
    reference: string
    gisData: GISData
    isQB: boolean
}

export interface LoadflowBranch {
    id: number
    branch: Branch
    voltage: number
    gisData1: GISData
    gisData2: GISData
}


export interface AllTripResult {
    surplus: number
    capacity: number
    trip: BoundaryTrip
    limCCt: string[]
    ctrls: CtrlResult[]
}

/**
 *  background tasks
 */
 export enum TaskStateEnum { Running = 0, Finished = 1 }
 export interface TaskState {
    taskId: number
    state: TaskStateEnum
    message: string
    progress: number
 }

 /**
  * About
  */
export enum ModificationTypeEnum  {Enhancement, Bug}

 export interface Version {
     name: string 
     modifications: Modification []
}

 export interface Modification {
     type: ModificationTypeEnum,
     description: string
 }

 /**
  * Elsi
  */
export enum ElsiPeriod {
    Pk, // peak 1 or 2 demand hours per day, 
    Pl, // daytime plateau demand – hours of daytime activity
    So, // daytime solar peak generation period (reduces plateau demand)
    Pu, // pick up/drop off period – transition from night trough to daytime plateau 
    Tr  // night time trough demand
}
export enum ElsiProfile {
    GB, NI, NO, DKe, DKw, NL, BE, FR, DE, IE}
export enum ElsiZone {
    BE, DE, DKe, DKw, FR, GB_EA, GB_EA_Dx, GB_MC, GB_MC_Dx, GB_NW, GB_NW_Dx, GB_SC, GB_SC_Dx, GB_SH, GB_SH_Dx, GB_SP, GB_SP_Dx, GB_UN, GB_UN_Dx, IE, NI, NL, NO}
export enum ElsiMainZone {
    BE, DE, DKe, DKw, FR, GB_EA, GB_MC, GB_NW, GB_SC, GB_SH, GB_SP, GB_UN, IE, NI, NL, NO
}
export enum ElsiGenType {Battery,  Biofuels,  Curtail,  Gas,  HardCoal,  HydroPump,  HydroRun,  HydroTurbine,  WindOnShore, WindOffShore, SolarPv, SolarThermal, Lignite,  Nuclear,  Oil,  OtherNonRes,  OtherRes}
export enum ElsiScenario {CommunityRenewables, TwoDegrees, SteadyProgression, ConsumerEvolution}
export enum ElsiBalanceMechanismInfoType { Gen, Store, Link }
export enum ElsiStorageMode { Generation, Production }

export interface ElsiDayResult {
    day: number,
    year: number,
    season: string,
    scenario: ElsiScenario,
    periodResults: PeriodResult[],
    availability: AvailabilityResults,
    market: MarketResults,
    balance: BalanceResults,
    balanceMechanism: BalanceMechanismResults,
    mismatches: MismatchResults,
}

export interface AvailabilityResults {
    demandResults: DemandResult[],
    generatorResults: GeneratorResult[],
    storeResults: StoreResult[],
    linkResults: LinkResult[]
}

export interface MarketResults {
    generatorResults: GeneratorResult[],
    storeResults: StoreResult[],
    linkResults: LinkResult[],
    marginalPrices: MarginalPrice[],
    storePrices: StorePrice[],
    linkPrices: LinkPrice[],
    zoneEmissionRates: ZoneEmissionsRate[],
    miscData: MiscData
}

export interface BalanceResults extends MarketResults {
    productionCostDiffs: ProductionCostDiffs
}

export interface PeriodResult {
    hours: number,
    index: number
}

export interface DemandResult {
    zone: ElsiMainZone,
    zoneName: string,
    demands: number[]
}

export interface GeneratorResult {
    zone: ElsiMainZone,
    zoneName: string,
    genType: ElsiGenType,
    genTypeName: string,
    cost: number,
    capacity: number,
    capacities: number[]
}

export interface StoreResult extends GeneratorResult {
    mode: ElsiStorageMode,
    modeName: string
}

export interface LinkResult {
    name: string,
    from: LinkEndResult,
    to: LinkEndResult
}

export interface LinkEndResult {
    zone: ElsiMainZone,
    zoneName: string,
    cost: number,
    capacity: number,
    capacities: number[]
}

export interface MarginalPrice {
    zone: ElsiMainZone,
    zoneName: string,
    cost: number,
    capacity: number,
    prices: number[]
}

export interface StorePrice extends MarginalPrice {
    genType: ElsiGenType,
    genTypeName: string,
    mode: ElsiStorageMode,
    modeName: string
}

export interface LinkPrice {
    name: string,
    from: MarginalPrice,
    to: MarginalPrice,    
}

export interface ZoneEmissionsRate {
    zone: ElsiMainZone,
    zoneName: string,
    rates: number[],
}

export interface MiscData {
    iters: number[],
    productionCosts: number[],
    dayError: number
}

export interface ProductionCostDiffs {
    diffs: number[]
}

export interface BalanceMechanismResults {
    marketInfo: BalanceMechanismMarketInfo[]
}

export interface BalanceMechanismMarketInfo {
    marketName: string,
    info: BalanceMechanismInfo[],
    lossChange: number[],
    total: number[]
}

export interface BalanceMechanismInfo {
    zone: ElsiMainZone,
    zoneName: string,
    type: ElsiBalanceMechanismInfoType,
    typeName: string,
    storageMode: ElsiStorageMode,
    storageModeName: string,
    genType: ElsiGenType,
    genTypeName: string,
    linkName: string,
    boa: number[],
    costs: number[]
}

export interface MismatchResults {
    market: number[],
    balance: number[]
}

export interface ElsiDataVersion {
    id: number,
    name: string,
    parent: ElsiDataVersion
}

export interface NewElsiDataVersion {
    name: string,
    parentId: number
}

export interface ElsiGenParameter {
    id: number,
    type: ElsiGenType,
    typeStr: string,
    efficiency: number,
    emissionsRate: number,
    forcedDays: number,
    plannedDays: number,
    maintenanceCost: number,
    fuelCost: number,
    warmStart: number,
    wearAndTearStart: number,
    endurance: number | null,
}

export interface ElsiGenCapacity {
    id: number,
    key: string,
    zone: ElsiZone,
    zoneStr: string,
    mainZone: ElsiMainZone,
    genType: ElsiGenType,
    genTypeStr: string,
    name: string,
    profile: ElsiProfile,
    profileStr: string,
    scenario: ElsiScenario,
    capacity: number,
    orderIndex: number | null,
}

export interface ElsiUserEdit {
    id: number,
    key: string,
    tableName: string,
    columnName: string,
    value: string
}

export interface DatasetInfo {
    genParameterInfo: TableInfo<ElsiGenParameter>
    genCapacityInfo: TableInfo<ElsiGenCapacity>
    peakDemandInfo: TableInfo<ElsiPeakDemand>
    miscParamsInfo: TableInfo<ElsiMiscParams>
    linkInfo: TableInfo<ElsiLink>
}

export interface TableInfo<T> {
    tableName: string
    data: T[]
    userEdits: ElsiUserEdit[]
}

export interface ElsiPeakDemand {
    id: number,
    mainZone: ElsiMainZone,
    mainZoneStr: string,
    profile: ElsiProfile,
    profileStr: string,
    scenario: ElsiScenario,
    peak: number
}

export interface ElsiMiscParams {
    id: number,
    eU_CO2: number,
    gB_CO2: number,
    vll: number
    gbpConv: number
}

export interface ElsiLink {
    id: number,
    name: string,
    fromZone: ElsiMainZone,
    fromZoneStr: string,
    toZone: ElsiMainZone,
    toZoneStr: string,
    capacity: number,
    revCap: number,
    loss: number,
    market: boolean,
    itf: number,
    itt: number,
    btf: number,
    btt: number
}

export interface ElsiResult {
    id: number,
    day: number,
    scenario: ElsiScenario
}

export interface ElsiProgress {
    numComplete: number,
    numToDo: number,
    percentComplete: number
}


/* users */
export enum UserRole {Basic, Admin}
export interface NewUser {
    email: string,
    name: string,
    password: string,
    confirmPassword: string
}

export interface Logon {
    email: string,
    password: string,
}

export interface User {
    id: number
    email: string
    name: string
    enabled : boolean
    role: UserRole
    roleStr: string
}

export interface ChangePassword {
    password: string
    newPassword1: string
    newPassword2: string
}

export interface DataModel {
    rows: DataRow[];
    size: string;
}

export interface DataRow {
    geoGraphicalArea: string;
    dno: string;
    dnoIconUrl: string;
    numGsps: number;
    numPrimary: number;
    numDist: number;
}

/* National grid */
export interface GridSubstation {
    id: number,
    name: string,
    reference: string,
    voltage: string,
    loadflowNode: Node,
    gisData: GISData    
}





