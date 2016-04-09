using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using System;
using System.Drawing;

//Our namespace (aka folder) where we keep our callout classes.
namespace StreetCallouts.Callouts
{
    // A pedestrian on a BMX bike whom may or may not be carrying drugs on their person ;)
    //Give your callout a string name and a probability of spawning. We also inherit from the Callout class, as this is a callout
    [CalloutInfo("SuspiciousPerson1", CalloutProbability.VeryHigh)]
    public class SuspiciousPerson1 : Callout
    {
        //Here we declare our variables, things we need or our callout
        private string[] pedList = new string[] {"A_F_Y_Hippie_01", "A_M_Y_Skater_01", "A_M_M_FatLatin_01", "A_M_M_EastSA_01", "A_M_Y_Latino_01", "G_M_Y_FamDNF_01", "G_M_Y_FamCA_01", "G_M_Y_BallaSout_01", "G_M_Y_BallaOrig_01", "G_M_Y_BallaEast_01", "G_M_Y_StrPunk_02", "S_M_Y_Dealer_01", "A_M_M_RurMeth_01", "A_M_Y_MethHead_01", "A_M_M_Skidrow_01", "S_M_Y_Dealer_01", "a_m_y_mexthug_01", "G_M_Y_MexGoon_03", "G_M_Y_MexGoon_02", "G_M_Y_MexGoon_01", "G_M_Y_SalvaGoon_01", "G_M_Y_SalvaGoon_02", "G_M_Y_SalvaGoon_03", "G_M_Y_Korean_01", "G_M_Y_Korean_02", "G_M_Y_StrPunk_01" };
        private string[] copVehicles = new string[] { "police", "police2", "police3", "police4", "fbi", "fbi2" };
        private string[] NotAcceptedResponses = new string[] { "OTHER_UNIT_TAKING_CALL_01", "OTHER_UNIT_TAKING_CALL_02", "OTHER_UNIT_TAKING_CALL_03", "OTHER_UNIT_TAKING_CALL_04", "OTHER_UNIT_TAKING_CALL_05", "OTHER_UNIT_TAKING_CALL_06", "OTHER_UNIT_TAKING_CALL_07" };
        private string[] CiviliansReporting = new string[] {"CITIZENS_REPORT_01", "CITIZENS_REPORT_02", "CITIZENS_REPORT_03", "CITIZENS_REPORT_04"};
        private string[] AConjunctive = new string[] {"A_01", "A_02"};
        private Ped subject;                    // our suspicious person
        private Vehicle bmxBike;                // his mode of travel
        private Vector3 SpawnPoint;             // area where suspicious person was spotted
        private Blip myBlip;                    // a gta v blip
        private LHandle pursuit;                // an API pursuit handle for any potential pursuits that occur
        private int scenario = 1;               // random scenario generated below
        private RelationshipGroup subjectGroup;
        private RelationshipGroup playerGroup;
        bool hasDrugs = false;
        bool enRoute = false;
        int storyLine = 1;
        bool startedPursuit = false;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            //Set the spawn point of the crime to be on a street around 500f (distance) away from the player.
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            while (SpawnPoint.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 200f)
            {
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            }

            // create the suspicious person
            subject = new Ped(this.pedList[Common.myRand.Next((int)pedList.Length)],SpawnPoint, 0f);

            // create his mode of transportation
            bmxBike = new Vehicle("bmx", SpawnPoint);

            // warp him onto it
            subject.WarpIntoVehicle(bmxBike, -1);

            switch (Common.myRand.Next(1,4))
            {
                case 1:
                    break;
                case 2:
                    // 1 in 4 chance that he has a controlled substance on his person
                    hasDrugs = true;
                    break;
                case 3:
                    break;
                case 4:
                    break;
            }

            // check for any errors
            if (!subject.Exists()) return false;
            if (!bmxBike.Exists()) return false;

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 100f);
            this.AddMinimumDistanceCheck(10f, subject.Position);

            // Set up our callout message and location
            this.CalloutMessage = "Suspicious Person\nNOTE: Subject on bicycle. Possible distribution of controlled substance.";
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout
            Functions.PlayScannerAudioUsingPosition(CiviliansReporting[Common.myRand.Next((int)CiviliansReporting.Length)] + " " + AConjunctive[Common.myRand.Next((int)AConjunctive.Length)] + " SUSPICIOUS PERSON" + " IN_OR_ON_POSITION", SpawnPoint);


            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            //We accepted the callout, so lets initilize our blip from before and attach it to our perp(s) so we know where he is.
            myBlip = subject.AttachBlip();
            myBlip.Color = Color.Yellow;

            // make subject cruise around
            subject.Tasks.CruiseWithVehicle(bmxBike, 16f, VehicleDrivingFlags.AllowWrongWay);

            enRoute = true;

            Game.DisplaySubtitle("Contact the ~r~subject.", 6500);

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (subject.Exists()) subject.Delete();
            if (bmxBike.Exists()) bmxBike.Delete();
            if (myBlip.Exists()) myBlip.Delete();
           
            // have another unit "respond" to it
            Functions.PlayScannerAudio(this.NotAcceptedResponses[Common.myRand.Next((int)this.NotAcceptedResponses.Length)]);
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            if(subject.DistanceTo(Game.LocalPlayer.Character) < 55f)
            {
                if (hasDrugs == true && startedPursuit == false)
                {
                    this.pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(this.pursuit, subject);
                    startedPursuit = true;
                }
                else if (subject.DistanceTo(Game.LocalPlayer.Character) < 30f && Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn && startedPursuit == false)
                {
                    subject.Tasks.Clear();
                    subject.Tasks.AchieveHeading(180f + (Game.LocalPlayer.Character.Heading), 3000);
                }

                if(hasDrugs == false && subject.DistanceTo(Game.LocalPlayer.Character) < 13f && Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                {
                    switch(storyLine)
                    {
                        case 1:
                            Game.DisplaySubtitle("Press ~y~Y ~w~to speak with subject", 5000);
                            GameFiber.Wait(5000);
                            Game.DisplaySubtitle("I was just going to my friends house sir! He lives right down the road!", 5000);
                            storyLine++;
                            break;
                        case 2:
                            Game.DisplaySubtitle("~b~You: ~w~Okay, relax buddy. Where are you coming from?", 5000);
                            storyLine++;
                            break;
                        case 3:
                            Game.DisplaySubtitle("I just came from the Ring Of Fire around the corner! Here's the receipt sir!", 5000);
                            storyLine++;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (!subject.IsAlive)
            {
                this.End();
            }
            if (this.pursuit != null && !Functions.IsPursuitStillRunning(this.pursuit))
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

            if (subject.Exists()) subject.Dismiss();
            if (bmxBike.Exists()) bmxBike.Dismiss();
            if (myBlip.Exists()) myBlip.Delete();

        }
    }
}
