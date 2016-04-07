﻿using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using System;
using System.Drawing;

//Our namespace (aka folder) where we keep our callout classes.
namespace StreetCallouts.Callouts
{
    //Give your callout a string name and a probability of spawning. We also inherit from the Callout class, as this is a callout
    [CalloutInfo("Stolen Vehicle", CalloutProbability.VeryHigh)]
    public class StolenVehicle : Callout
    {
        //Here we declare our variables, things we need or our callout
        private string[] pedList = new string[] {"a_m_y_mexthug_01", "G_M_Y_MexGoon_03", "G_M_Y_MexGoon_02", "G_M_Y_MexGoon_01", "G_M_Y_SalvaGoon_01", "G_M_Y_SalvaGoon_02", "G_M_Y_SalvaGoon_03", "G_M_Y_Korean_01", "G_M_Y_Korean_02", "G_F_Y_ballas_01", "G_M_Y_StrPunk_01"};
        private string[] vehicles = new string[] {"granger", "cavalcade", "cavalcade2", "baller2", "sadler", "speedo", "pony", "minivan", "bison", "bobcatxl", "burrito", "oracle2", "sultan", "futo", "banshee", "feltzer2", "elegy2", "jackal", "prairie", "zion", "zion2", "sentinel", "sentinel2", "penumbra", "buffalo2", "buffalo", "schwarzer", "dominator", "ruiner", "picador"};
        private Vehicle perpVehicle; // a rage vehicle
        private Vehicle backupVehicle; // back up
        private Ped perp1; // our criminals
        private Ped perp2;
        private Ped backupOfficer1; // unit who ran the plates
        private Vector3 SpawnPoint; // area where vehicle was spotted
        private Blip myBlip; // a gta v blip
        private LHandle pursuit; // an API pursuit handle for when the car flees
        private int scenario = 1; // random scenario generated below
        private bool OutOfCarFlag = false; // helps with the logic of scenario 2

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            scenario = Common.myRand.Next(1, 3);
            // scenarios:
            // 1 - one man, unarmed pursuit
            // 2 - one man, armed with a knife pursuit
            // 3 - two suspects, unarmed pursuit

            //Set the spawn point of the crime to be on a street around 320f (distance) away from the player.
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(320f));
            while(SpawnPoint.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 150f)
            {
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(320f));
            }

            //Create our criminal(s) in the world
            perp1 = new Ped(this.pedList[Common.myRand.Next((int)this.pedList.Length)], SpawnPoint, 0f);
            if (scenario == 3)
                perp2 = new Ped(this.pedList[Common.myRand.Next((int)this.pedList.Length)], SpawnPoint, 0f);

            if (scenario == 2)
                NativeFunction.Natives.GiveWeaponToPed(perp1, 0x99B507EA, 1, true, true);

            //Create the stolen vehicle
            perpVehicle = new Vehicle(this.vehicles[Common.myRand.Next((int)this.vehicles.Length)], SpawnPoint);

            // Create the unit who "ran the plates" of the stolen vehicle
            backupVehicle = new Vehicle("POLICE4", SpawnPoint.Around(25f));
            backupOfficer1 = new Ped("S_M_Y_Cop_01", SpawnPoint, 0f);

            // Now that we have spawned them, check they actually exist and if not return false (preventing the callout from being accepted and aborting it)
            if (!perp1.Exists()) return false;
            if (!perpVehicle.Exists()) return false;
            if (!backupOfficer1.Exists()) return false;
            if (scenario == 3 && !perp2.Exists()) return false;

            //If we made it this far everything exists so let's warp the ped(s) into the car
            NativeFunction.Natives.SetPedAsCop(backupOfficer1, true);
            backupOfficer1.WarpIntoVehicle(backupVehicle, -1);
            perp1.WarpIntoVehicle(perpVehicle, -1);
            if(scenario == 3)
                perp2.WarpIntoVehicle(perpVehicle, 0);

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 65f);
            this.AddMinimumDistanceCheck(10f, perp1.Position);

                 // Set up our callout message and location
            this.CalloutMessage = "Stolen vehicle" + "\nVEHICLE DESCRIPTION: " + perpVehicle.Model.Name + "\nPLATE #: " + perpVehicle.LicensePlate;
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout
            Functions.PlayScannerAudioUsingPosition("OFFICERS_REPORT_03 CRIME_GRAND_THEFT_AUTO_04 IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our perp(s) so we know where he is.
            myBlip = perp1.AttachBlip();
            if (scenario == 3)
                myBlip = perp2.AttachBlip();
            myBlip.Color = Color.Red;
            this.pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(this.pursuit, this.perp1);
            if (scenario == 3)
                Functions.AddPedToPursuit(this.pursuit, this.perp2);
            Functions.AddCopToPursuit(this.pursuit, backupOfficer1);
            for(int x = 0; x < Common.myRand.Next(0,3); ++x)
            {
                Functions.RequestBackup(this.perp1.GetOffsetPosition(Vector3.RelativeBack), LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
            }

            Game.DisplaySubtitle("Hurry up! ~y~Assist ~w~your fellow officers!", 6500);

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (perp1.Exists()) perp1.Delete();
            if(scenario == 3)
                if (perp2.Exists()) perp2.Delete();
            if (perpVehicle.Exists()) perpVehicle.Delete();
            if (backupVehicle.Exists()) backupVehicle.Delete();
            if (myBlip.Exists()) myBlip.Delete();
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            if(scenario == 2)
            {
                if (perp1.IsOnFoot && OutOfCarFlag == false)
                {
                    perp1.Tasks.FightAgainst(Game.LocalPlayer.Character);
                    OutOfCarFlag = true;
                }
            }

            //A simple check, if our pursuit has ended we end the callout
            if (!Functions.IsPursuitStillRunning(pursuit))
            {
                this.End();
            }
        }

        /// <summary>
        /// More cleanup, when we call 
        /// end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            base.End();
            if (myBlip.Exists()) myBlip.Delete();
            if (perp1.Exists()) perp1.Dismiss();
            if(scenario == 3)
                if (perp2.Exists()) perp2.Dismiss();
            if (perpVehicle.Exists()) perpVehicle.Dismiss();
            if (backupVehicle.Exists()) backupVehicle.Dismiss();

        }
    }
}
