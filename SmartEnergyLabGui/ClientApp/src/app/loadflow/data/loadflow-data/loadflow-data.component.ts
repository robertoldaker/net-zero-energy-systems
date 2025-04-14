import { AfterViewInit, Component, OnDestroy, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { MatTabGroup, MatTabLabel } from '@angular/material/tabs';
import { AllTripResult, SetPointMode } from 'src/app/data/app.data';
import { LoadflowDataBranchesComponent } from '../loadflow-data-branches/loadflow-data-branches.component';
import { LoadflowDataNodesComponent } from '../loadflow-data-nodes/loadflow-data-nodes.component';
import { LoadflowDataCtrlsComponent } from '../loadflow-data-ctrls/loadflow-data-ctrls.component';
import { LoadflowDataLocationsComponent } from '../loadflow-data-locations/loadflow-data-locations.component';
import { LoadflowMapComponent } from '../../loadflow-map/loadflow-map.component';

@Component({
    selector: 'app-loadflow-data',
    templateUrl: './loadflow-data.component.html',
    styleUrls: ['./loadflow-data.component.css']
})
export class LoadflowDataComponent extends ComponentBase implements AfterViewInit {

    private readonly NODES_INDEX = 0
    private readonly BRANCHES_INDEX = 1
    private readonly CTRLS_INDEX = 2
    private readonly LOCATIONS_INDEX = 5

    constructor(private dataService: LoadflowDataService) { 
        super()
        this.showAllTripResults = false;
        this.selected = new FormControl(0);
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.showAllTripResults = this.hasTrips(results.singleTrips) || this.hasTrips(results.doubleTrips) || this.hasTrips(results.intactTrips)
            this.hasNodesError = results.nodeMismatchError
            this.hasBranchesError = results.branchCapacityError
            this.hasCtrlsError = results.setPointError
            if ( this.matTabGroup ) {
                if ( this.hasNodesError) {
                    this.matTabGroup.selectedIndex = this.NODES_INDEX
                } else if ( this.hasBranchesError) {
                    this.matTabGroup.selectedIndex = this.BRANCHES_INDEX
                } else if ( this.hasCtrlsError) {
                    this.matTabGroup.selectedIndex = this.CTRLS_INDEX
                }
            }
        }))
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results)=>{
            this.hasNodesError = false
            this.hasBranchesError = false
            this.hasCtrlsError = false
            this.showAllTripResults = false;
        }))
        this.addSub( dataService.SetPointModeChanged.subscribe( (sp)=>{
            if ( sp == SetPointMode.Manual && this.matTabGroup) {
                this.matTabGroup.selectedIndex = this.CTRLS_INDEX
            }
        }))
    }
    
    ngAfterViewInit(): void {
        // dispatch this so that app-div-auto-scroller can detect size change
        // need to do this otherwise the location of the div is calculated incorrectly
        window.setTimeout(()=>{
            window.dispatchEvent(new Event('resize'));
        })
    }
    
    get mapButtonImage(): string {
        return this.showMap ? '/assets/images/table.png' : '/assets/images/world.png'
    }

    hasTrips(allTripResults: AllTripResult[]): boolean {
        return allTripResults!=null && allTripResults.length > 0
    }

    showAllTripResults: boolean
    selected: FormControl
    showMap:boolean = false;
    hasNodesError: boolean = false
    hasBranchesError: boolean = false
    hasCtrlsError: boolean = false
    @ViewChild(MatTabGroup) 
    matTabGroup: MatTabGroup | null = null;
    @ViewChild(LoadflowDataBranchesComponent)
    branchesComponent: LoadflowDataBranchesComponent | null = null;
    @ViewChild(LoadflowDataNodesComponent)
    nodesComponent: LoadflowDataNodesComponent | null = null;
    @ViewChild(LoadflowDataCtrlsComponent)
    ctrlsComponent: LoadflowDataCtrlsComponent | null = null;
    @ViewChild(LoadflowDataLocationsComponent)
    locationsComponent: LoadflowDataLocationsComponent | null = null;
    @ViewChild(LoadflowMapComponent)
    mapComponent: LoadflowMapComponent | null = null;

    toggleMap() {
        this.showMap = !this.showMap;
        if ( this.showMap ) {
            window.setTimeout( () => {
                if ( this.mapComponent) {
                    this.mapComponent.redraw();
                }
            },0 )    
        }
    }

    tabChange(e: any) {
        // dispatch this so that app-div-auto-scroller can detect size change
        window.dispatchEvent(new Event('resize'));
    }

    getNodeTabLabelClass():string {
        let tabClass = 'matTabLabelNarrow'
        if (this.hasNodesError) {
            tabClass += ' tabLabelError'
        }
        return tabClass
    }

    getBranchTabLabelClass():string {
        let tabClass = 'matTabLabelNarrow'
        if (this.hasBranchesError) {
            tabClass += ' tabLabelError'
        }
        return tabClass
    }

    getCtrlTabLabelClass():string {
        let tabClass = 'matTabLabelNarrow'
        if (this.hasCtrlsError) {
            tabClass += ' tabLabelError'
        }
        return tabClass
    }

    showNode(nodeCode: string) {
        if ( this.matTabGroup) {
            this.showMap = false
            if ( this.nodesComponent) {
                this.nodesComponent.filterByNode(nodeCode)
            }
            this.matTabGroup.selectedIndex = this.NODES_INDEX
        }
    }

    showBranchesForNode(nodeCode: string) {
        if ( this.matTabGroup) {
            this.showMap = false
            if ( this.branchesComponent) {
                this.branchesComponent.filterByNode(nodeCode)
            }
            this.matTabGroup.selectedIndex = this.BRANCHES_INDEX
        }
    }

    showCtrl(code: string) {
        if ( this.matTabGroup) {
            this.showMap = false
            if ( this.ctrlsComponent) {
                this.ctrlsComponent.filterByCode(code)
            }
            this.matTabGroup.selectedIndex = this.CTRLS_INDEX
        }
    }

    showBranch(code: string) {
        if ( this.matTabGroup) {
            this.showMap = false
            if ( this.branchesComponent) {
                this.branchesComponent.filterByCode(code)
            }
            this.matTabGroup.selectedIndex = this.BRANCHES_INDEX
        }
    }

    showLocation(locName: string) {
        if ( this.matTabGroup ){
            this.showMap = false
            if ( this.locationsComponent) {
                this.locationsComponent.filterByName(locName)
            }
            this.matTabGroup.selectedIndex = this.LOCATIONS_INDEX
        }
    }

    showLocationOnMap(locName: string) {
        this.showMap = true
        this.dataService.selectLocationByName(locName)
    }

    showLocationOnMapById(locId: number) {
        this.showMap = true
        this.dataService.selectLocation(locId)
    }

    showBranchOnMap(node1LocationId: number, node2LocationId: number) {
        this.showMap = true
        this.dataService.selectLinkByLocIds(node1LocationId,node2LocationId)
    }
}
