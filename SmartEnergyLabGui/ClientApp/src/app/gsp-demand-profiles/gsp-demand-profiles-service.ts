import { EventEmitter, Injectable } from "@angular/core"
import { DataClientService } from "../data/data-client.service";
import { GridSubstationLocation, GspDemandProfileData } from "../data/app.data";

@Injectable({
    providedIn: 'root'
})
export class GspDemandProfilesService {

    constructor(private dataClientService: DataClientService) {
        dataClientService.GetGspDemandDates((dates)=>{
            this.dates = []
            dates.forEach(element => {
                this.dates.push(new Date(element))
            });
            let endDate = this.dates[this.dates.length - 1]
            this.selectDate(endDate)
            this.DatesLoaded.emit(this.dates)
        });
    }

    dates: Date[] = []
    locations: GridSubstationLocation[] = []
    selectedLocation: GridSubstationLocation | undefined
    selectedDate: Date | undefined
    gbTotalProfile: number[] = []
    groupTotalProfile: number[] = []
    gspTotalProfile: number[] = []
    selectedGroupId: string = ''
    selectedArea: string = ''
    selectedGspId: string = ''
    selectedGspCode: string = ''
    gspProfiles: GspDemandProfileData[] = []
    gspProfileMap: Map<number,GspDemandProfileData[]> = new Map()

    createGspProfileMap(gspProfiles: GspDemandProfileData[]): { map: Map<number,GspDemandProfileData[]>, locs: GridSubstationLocation[], profiles: GspDemandProfileData[] }{
        let map = new Map <number,GspDemandProfileData[]>()
        let locs:GridSubstationLocation[] = []
        for( let gp of gspProfiles) {
            if ( gp.location) {
                if ( !map.has(gp.location.id)) {
                    map.set(gp.location.id,[])
                    locs.push(gp.location)
                }
                map.get(gp.location.id)?.push(gp)
            }
        }
        return { map: map, locs: locs, profiles: gspProfiles }
    }

    selectLocation(loc: GridSubstationLocation | undefined) {
        this.selectedLocation = loc
        if (this.selectedDate) {
            let profiles = this.getDemandProfiles(loc)
            this.setSelectedGspData(profiles)
            this.GspProfilesLoaded.emit(profiles)
            this.LocationSelected.emit(this.selectedLocation)
            this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, this.selectedGroupId, (profile) => {
                this.groupTotalProfile = profile
                this.GspGroupTotalProfileLoaded.emit(profile)
            })
        }
    }

    getDemandProfiles(loc: GridSubstationLocation | undefined):GspDemandProfileData[] {
        if ( loc ) {
            let profiles = this.gspProfileMap.get(loc.id)
            if (profiles) {
                return profiles
            } else {
                return []
            }
        } else {
            return []
        }
    }

    setSelectedGspData(profiles: GspDemandProfileData[]) {
        this.gspTotalProfile = []
        this.selectedGspId = ''
        for( let p of profiles) {
            //
            if ( this.selectedGspId === '') {
                this.selectedGspId=p.gspId
            } else {
                this.selectedGspId+=` + ${p.gspId}`
            }
            //
            if (this.gspTotalProfile.length === 0) {
                this.gspTotalProfile = p.demand
            } else {
                for( let i=0;i<p.demand.length;i++) {
                    this.gspTotalProfile[i] += p.demand[i]
                }
            }
        }
        if ( profiles.length>0 ) {
            this.selectedGroupId = profiles[0].gspGroupId
            this.selectedArea = profiles[0].gspArea
            this.selectedGspCode = profiles[0].gspCode
        } else {
            this.selectedGroupId = ''
            this.selectedArea = ''
            this.selectedGspCode = ''
        }
    }

    selectDate(date: Date) {
        this.selectedDate = date
        this.dataClientService.GetTotalGspDemandProfile(date, '', (profile) => {
            this.gbTotalProfile = profile
            this.GBTotalProfileLoaded.emit(profile)
        })
        this.dataClientService.GetGspDemandProfiles(date, date, '', (gspProfiles) => {
            let d = this.createGspProfileMap(gspProfiles)
            this.gspProfileMap = d.map
            this.locations = d.locs
            this.gspProfiles = d.profiles
            this.LocationsLoaded.emit(this.locations)
            //
            // re-select location which will re-calculate gsp and group total profiles
            if ( this.selectedLocation) {
                this.selectLocation(this.selectedLocation)
            }
        });
    }

    isLocationGroupSelected(loc: GridSubstationLocation):boolean {
        let profiles = this.gspProfileMap.get(loc.id)
        if ( profiles && this.selectedGroupId ) {
            let p = profiles.find(m=>m.gspGroupId === this.selectedGroupId);
            return p ? true : false
        } else {
            return false
        }
    }

    searchProfiles(searchStr: string, maxResults: number):GspDemandProfileData[] {
        let lcSearchStr = searchStr.toLowerCase()
        let profiles = this.gspProfiles.
            filter(m => m.gspId.toLowerCase().startsWith(lcSearchStr) || (m.location && m.location.name?.toLocaleLowerCase().includes(lcSearchStr))).slice(0, maxResults)
        return profiles
    }

    LocationsLoaded:EventEmitter<GridSubstationLocation[]> = new EventEmitter<GridSubstationLocation[]>()
    DatesLoaded: EventEmitter<Date[]> = new EventEmitter<Date[]>()
    GspProfilesLoaded: EventEmitter<GspDemandProfileData[]> = new EventEmitter<GspDemandProfileData[]>()
    GBTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    GspGroupTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    LocationSelected:EventEmitter<GridSubstationLocation | undefined> = new EventEmitter<GridSubstationLocation | undefined>()

}
