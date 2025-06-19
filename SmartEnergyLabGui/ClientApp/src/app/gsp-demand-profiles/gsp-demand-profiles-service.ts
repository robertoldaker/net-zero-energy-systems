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
        /*dataClientService.GetGspDemandLocations((locs) => {
            this.locations = locs
            this.LocationsLoaded.emit(this.locations)
        });*/
    }

    dates: Date[] = []
    locations: GridSubstationLocation[] = []
    selectedLocation: GridSubstationLocation | undefined
    selectedProfile: GspDemandProfileData | undefined
    selectedDate: Date | undefined
    gbTotalProfile: number[] = []
    groupTotalProfile: number[] = []
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
                    this.selectedProfile = profiles[0]
                    this.GspProfileLoaded.emit(profiles[0])
                    this.LocationSelected.emit(this.selectedLocation)
                    this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, this.selectedProfile.gspGroupId, (profile) => {
                        this.groupTotalProfile = profile
                        this.GspGroupTotalProfileLoaded.emit(profile)
                    })
                }
            })
        }
    }

    selectDate(date: Date | undefined) {
        this.selectedDate = date
        if ( this.selectedDate ) {
            this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, '', (profile) => {
                this.gbTotalProfile = profile
                this.GBTotalProfileLoaded.emit(profile)
            })
            // re-load selectedProfile for new date
            if ( this.selectedProfile ) {
                // gsp
                this.dataClientService.GetGspDemandProfiles(this.selectedDate, this.selectedDate, this.selectedProfile.gspCode, (profiles) => {
                    if (profiles.length > 0 && this.selectedDate) {
                        this.selectedProfile = profiles[0]
                        this.GspProfileLoaded.emit(profiles[0])
                    }
                })
                // gsp group total
                this.dataClientService.GetTotalGspDemandProfile(this.selectedDate, this.selectedProfile.gspGroupId, (profile) => {
                    this.groupTotalProfile = profile
                    this.GspGroupTotalProfileLoaded.emit(profile)
                })
            }
        }
    }

    isLocationGroupSelected(loc: GridSubstationLocation):boolean {
        let profiles = this.gspProfileMap.get(loc.id)
        if ( profiles && this.selectedProfile ) {
            let p = profiles.find(m=>m.gspGroupId === this.selectedProfile?.gspGroupId);
            return p ? true : false
        } else {
            return false
        }
    }

    LocationsLoaded:EventEmitter<GridSubstationLocation[]> = new EventEmitter<GridSubstationLocation[]>()
    DatesLoaded: EventEmitter<Date[]> = new EventEmitter<Date[]>()
    GspProfileLoaded: EventEmitter<GspDemandProfileData> = new EventEmitter<GspDemandProfileData>()
    GBTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    GspGroupTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    LocationSelected:EventEmitter<GridSubstationLocation | undefined> = new EventEmitter<GridSubstationLocation | undefined>()

}
