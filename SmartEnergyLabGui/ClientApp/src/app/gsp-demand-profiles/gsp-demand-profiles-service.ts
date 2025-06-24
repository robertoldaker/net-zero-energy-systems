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
            dataClientService.GetGspDemandProfiles(endDate, endDate, '', (gspProfiles) => {
                let d = this.createGspProfileMap(gspProfiles)
                this.gspProfileMap = d.map
                this.locations = d.locs
                this.LocationsLoaded.emit(this.locations)
            });
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
    selectedGspId: string = ''
    selectedGspCode: string = ''
    gspProfileMap: Map<number,GspDemandProfileData[]> = new Map()

    createGspProfileMap(gspProfiles: GspDemandProfileData[]): { map: Map<number,GspDemandProfileData[]>, locs: GridSubstationLocation[] }{
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
        return { map: map, locs: locs }
    }

    selectLocation(loc: GridSubstationLocation) {
        this.selectedLocation = loc
        if ( this.selectedDate ) {
            this.dataClientService.GetGspDemandProfiles(this.selectedDate,this.selectedDate,loc.reference,(profiles)=>{
                if ( profiles.length>0 && this.selectedDate) {
                    this.setSelectedGspData(profiles)
                    this.GspProfilesLoaded.emit(profiles)
                    this.LocationSelected.emit(this.selectedLocation)
                    this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, this.selectedGroupId, (profile) => {
                        this.groupTotalProfile = profile
                        this.GspGroupTotalProfileLoaded.emit(profile)
                    })
                }
            })
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
        this.selectedGroupId = profiles[0].gspGroupId
        this.selectedGspCode = profiles[0].gspCode
    }

    selectDate(date: Date | undefined) {
        this.selectedDate = date
        if ( this.selectedDate ) {
            this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, '', (profile) => {
                this.gbTotalProfile = profile
                this.GBTotalProfileLoaded.emit(profile)
            })
            // re-load selectedProfile for new date
            if ( this.selectedGspCode ) {
                // gsp
                this.dataClientService.GetGspDemandProfiles(this.selectedDate, this.selectedDate, this.selectedGspCode, (profiles) => {
                    if (profiles.length > 0 && this.selectedDate) {
                        this.setSelectedGspData(profiles)
                        this.GspProfilesLoaded.emit(profiles)
                    }
                })
                // gsp group total
                this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, this.selectedGroupId, (profile) => {
                    this.groupTotalProfile = profile
                    this.GspGroupTotalProfileLoaded.emit(profile)
                })
            }
        }
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

    LocationsLoaded:EventEmitter<GridSubstationLocation[]> = new EventEmitter<GridSubstationLocation[]>()
    DatesLoaded: EventEmitter<Date[]> = new EventEmitter<Date[]>()
    GspProfilesLoaded: EventEmitter<GspDemandProfileData[]> = new EventEmitter<GspDemandProfileData[]>()
    GBTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    GspGroupTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    LocationSelected:EventEmitter<GridSubstationLocation | undefined> = new EventEmitter<GridSubstationLocation | undefined>()

}
