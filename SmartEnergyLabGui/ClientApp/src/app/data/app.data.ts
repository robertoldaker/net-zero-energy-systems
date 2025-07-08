/**
 * Data definitions for Admin
 */
export enum ApplicationGroup { All=0, BoundCalc=1, Elsi=2 }

export interface SystemInfo {
    processorCount: number,
    maintenanceMode: boolean
    versionData: VersionData
}

export interface VersionData {
    version: string,
    commitId: string,
    commitDate: string
}

/**
 * Data definitions for "Low voltage network"
 */
export enum DNOAreas {
    EastEngland,
    EastMidlands,
    London,
    NorthEastEngland,
    NorthScotland,
    NorthWales,
    NorthWestEngland,
    SouthEastEngland,
    SouthernEngland,
    SouthScotland,
    SouthWales,
    SouthWestEngland,
    WestMidlands,
    Yorkshire
}

export interface GridSupplyPoint {
    id: number,
    nrId: string,
    nr: string,
    name: string,
    gisData: GISData
    numberOfSolarInstallations: number
    numberOfPrimarySubstations: number,
    isDummy: boolean,
    needsNudge: boolean
    dnoArea: DNOAreas
}

export interface PrimarySubstation {
    id: number,
    gspId: number,
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
    substationData: DistributionSubstationData,
    classifications: SubstationClassification[]
    loadProfiles: SubstationLoadProfile[]
    substationParams: SubstationParams,
    chargingParams: SubstationChargingParams,
    heatingParams: SubstationHeatingParams
}

export enum DistributionSubstationType {Ground,Pole}

export interface DistributionSubstationData {
    id: number,
    type: DistributionSubstationType,
    hvFeeder: string,
    dayMaxDemand: number,
    nightMaxDemand: number,
    rating: number,
    numEnergyStorage: number,
    numHeatPumps: number,
    numEVChargers: number,
    totalLCTCapacity: number,
    totalGenerationCapacity: number,
    numCustomers: number
}

export interface SubstationSearchResult {
    id: number,
    parentId: number,
    name: string,
    type: string,
    key:string
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

export enum LoadNetworkDataSource {
    All,
    NGED,
    UKPower,
    NPG,
    SSEN
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
 * EV Demand tool
 */
export interface EVDemandStatus {
    isRunning: boolean,
    isReady: boolean
}

/**
 * BoundCalc
 */

export enum SetPointMode {Zero,Auto,Manual}

export interface LoadflowResults {
    stageResults: StageResults,
    nodes: DatasetData<Node>,
    branches: DatasetData<Branch>,
    ctrls: DatasetData<Ctrl>,
    boundaryTripResults: BoundaryTripResults
    boundaryTrips: BoundaryTrips,
    nodeMismatchError: boolean
    nodeMismatchErrorAsc: boolean
    branchCapacityError: boolean
    setPointError: boolean
}

export interface Boundary {
    id: number,
    code: string,
    zones: Zone[]
}

export enum BoundaryTripType { Single = 0, Double = 1}
export interface BoundaryTrip {
    index: number,
    type: BoundaryTripType,
    lineNames: string[],
    branchIds: number[],
    branchCodes: string[],
    text: string
}

export interface BoundaryTrips {
    trips: BoundaryTrip[],
    lineNames: string[]
}

export interface StageResults {
    results: StageResult[]
    nodeResults: NodeResult[]
    branchResults: BranchResult[]
    ctrlResults: CtrlResult[]
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

export interface Node {
    id: number
    datasetId: number
    code: string
    voltage: number
    name: string
    location: GridSubstationLocation | undefined
    demand: number
    generators: Generator[]
    deletedGenerators: Generator[]
    newGenerators: Generator[]
    generation: number
    generation_A: number
    generation_B: number
    ext: boolean
    zone: Zone | undefined
    zoneName: string
    tlf: number | undefined
    km: number | undefined
    mismatch: number | undefined
}

export enum GeneratorType
    {
        Biomass,
        CCGT,
        CHP,
        Coal,
        Hydro,
        Interconnector,
        Nuclear,
        OCGT,
        PumpStorage,
        Tidal,
        Wave,
        WindOffshore,
        WindOnshore
    }

export interface Generator {
    id: number
    datasetId: number
    name: string
    capacity: number
    nodeCount: number
    scaledGeneration: number
    scaledGenerationPerNode: number
    type: GeneratorType
    typeStr: string
}

export interface NodeGenerator {
    id: number
    datasetId: number
    nodeId: number,
    generatorId: number
}

export interface TransportModel {
    id: number
    name: string
    datasetId: number
}

export interface TransportModelEntry {
    id: number
    transportModelId: number
    generatorType: GeneratorType
    autoScaling: boolean
    scaling: number
    totalCapacity: number
}

export enum BranchType { Other, HVDC, OHL, Cable, Composite, Transformer, QB, SSSC, SeriesCapacitor, SeriesReactor }
export interface Branch {
    id: number
    datasetId: number
    displayName: string
    lineName: string
    type: BranchType
    typeStr: string
    ctrlId: number
    region: string
    code: string
    r: number
    x: number
    ohl: number
    cableLength: number
    totalLength: number
    cap: number
    linkType: string
    node1Code: string
    node2Code: string
    node1Name: string
    node2Name: string
    node1Voltage: number
    node2Voltage: number
    node1Id: number
    node2Id: number
    node1LocationId: number
    node2LocationId: number
    node1GISData: GISData | null
    node2GISData: GISData | null
    node1ZoneId: number
    node2ZoneId: number
    outaged: boolean
    powerFlow: number | null
    bFlow: number
    freePower: number | null
    km: number | null
    mwkm: number | null
    loss: number | null
    percentCapacity: number | null
}

export enum LoadflowCtrlType {  QB=0,  // Quad booster
                                HVDC=1 // High-voltage DC
                             }
export interface Ctrl {
    id: number
    displayName: string
    datasetId: number
    branchId: number
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
    node1LocationId: number
    node2LocationId: number
    node1: Node
    node2: Node
    setPoint: number | null
}

export interface Zone {
    id: number
    code: string
    datasetId: number
}

export interface IBoundaryDict {
    [index: string]: number[],
}
export interface NetworkData {
    nodes: DatasetData<Node>
    branches: DatasetData<Branch>
    ctrls: DatasetData<Ctrl>
    boundaries: DatasetData<Boundary>
    zones: DatasetData<Zone>
    locations: DatasetData<GridSubstationLocation>
    generators: DatasetData<Generator>
    transportModels: DatasetData<TransportModel>
    transportModelEntries: DatasetData<TransportModelEntry>
    transportModel: TransportModel | null
}

export enum GridSubstationLocationSource { NGET, SHET, SPT, GoogleMaps, Estimated, UserDefined}
export interface GridSubstationLocation {
    id: number
    datasetId: number
    name: string
    reference: string
    source: GridSubstationLocationSource
    latitude: number
    longitude: number
}

export interface BoundaryTripResults {
    singleTrips: AllTripResult[]
    doubleTrips: AllTripResult[]
    intactTrips: AllTripResult[]
    worstTrip: BoundaryTrip
}

export interface AllTripResult {
    surplus: number
    capacity: number
    trip: BoundaryTrip | null
    limCct: string[]
    ctrls: CtrlResult[]
}

export interface CtrlSetPoint {
    ctrlId: number,
    setPoint: number
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
  * Datasets
  */
 export enum DatasetType {Elsi, Loadflow, BoundCalc}

export interface IId {
    id: number
}

export interface Dataset {
    id: number,
    name: string,
    type: DatasetType,
    parent: Dataset | null
    isReadOnly: boolean
}

export interface NewDataset {
    name: string,
    parentId: number
}

export interface UserEdit {
    id: number,
    key: string,
    tableName: string,
    columnName: string,
    value: string,
    prevValue: string,
    newDatasetId: number
}

export interface DatasetData<T> {
    tableName: string
    data: T[]
    deletedData: T[]
    userEdits: UserEdit[]
}

export interface EditItem {
    className: string,
    id: number,
    datasetId: number,
    data?: any
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
    zone: ElsiZone,
    zoneStr: string,
    mainZone: ElsiMainZone,
    genType: ElsiGenType,
    genTypeStr: string,
    name: string,
    profile: ElsiProfile,
    profileStr: string,
    communityRenewables: number,
    twoDegrees: number,
    steadyProgression: number,
    consumerEvolution: number,
    scenario: ElsiScenario,
    capacity: number,
    orderIndex: number | null,
}

export interface ElsiDatasetInfo {
    genParameterInfo: DatasetData<ElsiGenParameter>
    genCapacityInfo: DatasetData<ElsiGenCapacity>
    peakDemandInfo: DatasetData<ElsiPeakDemand>
    miscParamsInfo: DatasetData<ElsiMiscParams>
    linkInfo: DatasetData<ElsiLink>
}

export interface ElsiPeakDemand {
    id: number,
    mainZone: ElsiMainZone,
    mainZoneStr: string,
    profile: ElsiProfile,
    profileStr: string,
    communityRenewables: number,
    twoDegrees: number,
    steadyProgression: number,
    consumerEvolution: number,
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

export interface ResetPassword {
    token: string
    newPassword1: string
    newPassword2: string
}

export interface DataModel {
    size: string;
    diskUsage: DiskUsage;
}

export interface DistributionData {
    rows: DistributionDataRow[];
}

export interface TransmissionData {
    rows: TransmissionDataRow[];
}

export interface DiskUsage {
    found: boolean
    total: number
    used: number
    available: number
}

export interface DistributionDataRow {
    geoGraphicalAreaId: number,
    geoGraphicalArea: string;
    dno: string;
    dnoIconUrl: string;
    numGsps: number;
    numPrimary: number;
    numDist: number;
}

export interface TransmissionDataRow {
    source : GridSubstationLocationSource
    sourceStr: string;
    sourceIconUrl: string;
    numLocations: number;
    numSubstations: number;
}

/* National grid */
export enum NationalGridNetworkSource {All, NGET, SSE, SPT}

export interface GridSubstation {
    id: number,
    name: string,
    reference: string,
    voltage: string,
    loadflowNode: Node,
    gisData: GISData
}

/* generic logs */
export interface ILogs {
    Logs(onComplete: (resp: any)=> void | undefined, onError: (resp:any)=>void): void
    Clear(onComplete: (resp: any)=> void): void
}

/* solar installations */
export interface SolarInstallation {
    id: number,
    year: number,
    gisData: GISData
}

/* GSP Demand profiles */
export interface GspDemandProfileData {
    id: number,
    gspId: string,
    gspCode: string,
    gspArea: string,
    gspGroupId: string,
    date: Date,
    demand: number[]
    isEstimate: boolean[]
    location: GridSubstationLocation | null
}



