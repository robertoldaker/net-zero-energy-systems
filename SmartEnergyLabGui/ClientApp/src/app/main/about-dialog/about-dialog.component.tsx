import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { ModificationTypeEnum, Version } from '../../data/app.data';
import { DialogFooterButtonsEnum } from '../../dialogs/dialog-footer/dialog-footer.component';
import { MainService } from '../main.service';

@Component({
    selector: 'app-about-dialog',
    templateUrl: './about-dialog.component.html',
    styleUrls: ['./about-dialog.component.css']
})
export class AboutDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<AboutDialogComponent>, @Inject('MODE') public mode: string, public mainService: MainService) {
        this.versions = [];
        this.versions.push( {
            name: "0.1",
            modifications: [
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Implemented viewing by month, and by season"
            },
            {
                type: ModificationTypeEnum.Bug,
                description: "Fixed issue with Signal-R not working on external connections to server"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Show data as load in Kw for both actual and prediction"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Run tool on a single substation"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description:"Allow users to search for and select a substation by substation name or id" 
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description:"Create an about screeen showing new bug fixes and features of each release"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Add polygon around geographical area"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Show data associated with selected substation using Google maps info marker"
            }
            ]
        })

        //
        this.versions.push( {
            name: "0.2",
            modifications: [
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added boundaries around each primary substation"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added vehicle charging stations"
            },
            ]
        })

        //
        this.versions.push( {
            name: "0.3",
            modifications: [
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added layers for Power, Vehicle Charging and Heat Pumps"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added placeholders for Carbon and Price Profile plots."
            },
            ]
        })

        //
        this.versions.push( {
            name: "0.4",
            modifications: [
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Removed layers for Vehicle Charging and Heat Pumps and added option to show \"Public EV charger\""
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added load profile graphs for Vehicle Charging and Heat Pumps"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added options to show load profile as Carbon (kg/h) and Cost (p/h)"
            }
            ]
        })

        this.versions.push( {
            name: "0.5",
            modifications: [
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Added ability to load EV and HP prediction data for years 2021-2050"
            },
            {
                type: ModificationTypeEnum.Enhancement,
                description: "Created a single control to change year and month for all load profile graphs"
            }
            ]
        })

        this.versions.reverse();
    }

    ngOnInit(): void {

    }

    versions: Version[]
    commitId: string = "$COMMIT_ID$"
    commitDate: string = "$COMMIT_DATE$"
    DialogFooterButtons: any = DialogFooterButtonsEnum
    ModificationTypeEnum: any = ModificationTypeEnum

}
