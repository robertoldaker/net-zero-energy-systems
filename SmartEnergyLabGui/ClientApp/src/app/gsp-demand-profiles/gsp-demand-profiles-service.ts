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
            this.selectDate(this.dates[this.dates.length-1])
            this.DatesLoaded.emit(this.dates)
        });
        dataClientService.GetGspDemandLocations((locs) => {
            this.locations = locs
            this.LocationsLoaded.emit(this.locations)
        });
    }

    dates: Date[] = []
    locations: GridSubstationLocation[] = []
    selectedLocation: GridSubstationLocation | undefined
    selectedProfile: GspDemandProfileData | undefined
    selectedDate: Date | undefined
    gbTotalProfile: number[] = []
    groupTotalProfile: number[] = []

    selectLocation(loc: GridSubstationLocation) {
        this.selectedLocation = loc
        if ( this.selectedDate ) {
            this.dataClientService.GetGspDemandProfiles(this.selectedDate,this.selectedDate,loc.reference,(profiles)=>{
                if ( profiles.length>0 && this.selectedDate) {
                    this.selectedProfile = profiles[0]
                    this.GspProfileLoaded.emit(profiles[0])
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
        }
    }

    LocationsLoaded:EventEmitter<GridSubstationLocation[]> = new EventEmitter<GridSubstationLocation[]>()
    DatesLoaded: EventEmitter<Date[]> = new EventEmitter<Date[]>()
    GspProfileLoaded: EventEmitter<GspDemandProfileData> = new EventEmitter<GspDemandProfileData>()
    GBTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()
    GspGroupTotalProfileLoaded: EventEmitter<number[]>=new EventEmitter<number[]>()

}
