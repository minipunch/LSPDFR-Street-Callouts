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
    [CalloutInfo("SuspiciousPerson1", CalloutProbability.Medium)]
    public class SuspiciousPerson1 : Callout
    {
        //Here we declare our variables, things we need or our callout
        private string[] pedList = new string[] { "A_F_Y_Hippie_01", "A_M_Y_Skater_01", "A_M_M_FatLatin_01", "A_M_M_EastSA_01", "A_M_Y_Latino_01", "G_M_Y_FamDNF_01", "G_M_Y_FamCA_01", "G_M_Y_BallaSout_01", "G_M_Y_BallaOrig_01", "G_M_Y_BallaEast_01", "G_M_Y_StrPunk_02", "S_M_Y_Dealer_01", "A_M_M_RurMeth_01", "A_M_Y_MethHead_01", "A_M_M_Skidrow_01", "S_M_Y_Dealer_01", "a_m_y_mexthug_01", "G_M_Y_MexGoon_03", "G_M_Y_MexGoon_02", "G_M_Y_MexGoon_01", "G_M_Y_SalvaGoon_01", "G_M_Y_SalvaGoon_02", "G_M_Y_SalvaGoon_03", "G_M_Y_Korean_01", "G_M_Y_Korean_02", "G_M_Y_StrPunk_01" };
        private string[] copVehicles = new string[] { "police", "police2", "police3", "police4", "fbi", "fbi2" };
        private string[] NotAcceptedResponses = new string[] { "OTHER_UNIT_TAKING_CALL_01", "OTHER_UNIT_TAKING_CALL_02", "OTHER_UNIT_TAKING_CALL_03", "OTHER_UNIT_TAKING_CALL_04", "OTHER_UNIT_TAKING_CALL_05", "OTHER_UNIT_TAKING_CALL_06", "OTHER_UNIT_TAKING_CALL_07" };
        private string[] CiviliansReporting = new string[] { "CITIZENS_REPORT_01", "CITIZENS_REPORT_02", "CITIZENS_REPORT_03", "CITIZENS_REPORT_04" };
        private string[] DispatchCopyThat = new string[] { "REPORT_RESPONSE_COPY_01", "REPORT_RESPONSE_COPY_02", "REPORT_RESPONSE_COPY_03", "REPORT_RESPONSE_COPY_04" };
        private string[] AConjunctive = new string[] { "A_01", "A_02" };
        private Ped subject;                    // our suspicious person
        private Vehicle bmxBike;                // his mode of travel
        private Vector3 SpawnPoint;             // area where suspicious person was spotted
        private Blip myBlip;                    // a gta v blip
        private LHandle pursuit;                // an API pursuit handle for any potential pursuits that occur
        private bool hasDrugs = false;
        private int storyLine = 1;
        private bool startedPursuit = false;
        private bool wasClose = false;
        private bool alreadySubtitleIntrod = false;
        private bool hasTalkedBack = false;
        private int callOutMessage = 0;

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
            subject = new Ped(this.pedList[Common.myRand.Next((int)pedList.Length)], SpawnPoint, 0f);

            // create his mode of transportation
            bmxBike = new Vehicle("bmx", SpawnPoint);

            // warp him onto it
            subject.WarpIntoVehicle(bmxBike, -1);

            switch (Common.myRand.Next(1, 3))
            {
                case 1:
                    break;
                case 2:
                    // 1 in 3 chance that he has a controlled substance on his person
                    hasDrugs = true;
                    break;
                case 3:
                    break;
            }

            // check for any errors
            if (!subject.Exists()) return false;
            if (!bmxBike.Exists()) return false;

            // Show the user where the pursuit is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 100f);
            this.AddMinimumDistanceCheck(10f, subject.Position);

            // Set up our callout message and location
            switch (Common.myRand.Next(1, 3))
            {
                case 1:
                    this.CalloutMessage = "Suspicious Person\nINFO: Subject on a bike peering into vehicles.";
                    callOutMessage = 1;
                    break;
                case 2:
                    this.CalloutMessage = "Suspicious Person\nINFO: Possibly distributing narcotics";
                    callOutMessage = 2;
                    break;
                case 3:
                    this.CalloutMessage = "Suspicious Person\nINFO: Possibly in possession of narcotics.";
                    callOutMessage = 3;
                    break;
            }
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout
            Functions.PlayScannerAudioUsingPosition(CiviliansReporting[Common.myRand.Next((int)CiviliansReporting.Length)] + " " + AConjunctive[Common.myRand.Next((int)AConjunctive.Length)] + " SUSPICIOUS_PERSON" + " IN_OR_ON_POSITION", SpawnPoint);


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

            Functions.PlayScannerAudio(this.DispatchCopyThat[Common.myRand.Next((int)this.DispatchCopyThat.Length)]);
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

            GameFiber.StartNew(delegate
            {
                if (subject.DistanceTo(Game.LocalPlayer.Character) < 25f)
                {
                    if (hasDrugs == true && startedPursuit == false)
                    {
                        this.pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(this.pursuit, subject);
                        startedPursuit = true;
                    }

                    if(subject.DistanceTo(Game.LocalPlayer.Character) < 15f && Game.LocalPlayer.Character.IsOnFoot && alreadySubtitleIntrod == false && pursuit == null)
                    {
                        Game.DisplaySubtitle("Press ~y~Y ~w~to speak with the subject", 5000);
                        alreadySubtitleIntrod = true;
                        wasClose = true;
                    }

                    if (hasDrugs == false && subject.DistanceTo(Game.LocalPlayer.Character) < 15f && Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        switch (storyLine)
                        {
                            case 1:
                                Game.DisplaySubtitle("~y~Suspect: ~w~I was just going to my friends house sir! He lives right down the road! (1/3)", 5000);
                                storyLine++;
                                break;
                            case 2:
                                Game.DisplaySubtitle("~b~You: ~w~Okay, relax buddy. Where are you coming from?", 5000);
                                storyLine++;
                                break;
                            case 3:
                                Game.DisplaySubtitle("~y~Suspect: ~w~I just came from the Ring Of Fire around the corner! Here's the receipt sir! (2/3)", 5000);
                                storyLine++;
                                break;
                            case 4:
                                if(callOutMessage == 1)
                                    Game.DisplaySubtitle("~b~You: ~w~How come we have people saying you've been peering into vehicles?", 5000);
                                if (callOutMessage == 2)
                                    Game.DisplaySubtitle("~b~You: ~w~Why do we have some people saying they saw someone dealing drugs around here?", 5000);
                                if (callOutMessage == 3)
                                    Game.DisplaySubtitle("~b~You: ~w~What if I told you I saw you making a hand to hand transaction with someone back a few blocks?", 5000);
                                storyLine++;
                                break;
                            case 5:
                                if (callOutMessage == 1)
                                    Game.DisplaySubtitle("~y~Suspect: ~w~Haha -- peering into vehicles? Sir i'm just riding my bike, sir... (3/3)", 5000);
                                if (callOutMessage == 2)
                                    Game.DisplaySubtitle("~y~Suspect: ~w~Sir -- no way i'm not like that, sir. (3/3)", 5000);
                                if (callOutMessage == 3)
                                    Game.DisplaySubtitle("~y~Suspect: ~w~Yeah, right! And where was that? I know my rights sir, my lawyer told me not to let cops search me. (3/3)", 5000);
                                storyLine++;
                                // random chance to flee during this part of interaction
                                if(Common.myRand.Next(1,4) == 4)
                                {
                                    this.pursuit = Functions.CreatePursuit();
                                    Functions.AddPedToPursuit(this.pursuit, subject);
                                    startedPursuit = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Press LCNTRL + LSHFT + Y to force end call out
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                {
                    if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LShiftKey))
                    {
                        if(Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LControlKey))
                        {
                            Game.DisplaySubtitle("~b~You: ~w~Dispatch we're code 4. Show me ~g~10-8.", 4000);
                            Functions.PlayScannerAudio(this.DispatchCopyThat[Common.myRand.Next((int)DispatchCopyThat.Length)]);
                            this.End();
                        }
                    }
                }

                if(subject.Exists() && Functions.IsPedArrested(subject) && hasDrugs && subject.DistanceTo(Game.LocalPlayer.Character) < 15f && !hasTalkedBack)
                {
                    Game.DisplaySubtitle("~y~Suspect: ~w~I'm sorry sir I didn't mean to run -- I was scared!", 4000);
                    hasTalkedBack = true;
                }
            
                // END CONDITIONS
                if(subject.DistanceTo(Game.LocalPlayer.Character) >= 200f && wasClose)
                {
                    if(startedPursuit)
                    {
                        Game.DisplaySubtitle("~y~Suspect ~w~has escaped.", 4300);
                    }
                    this.End();
                }
                if (subject.IsDead || !subject.Exists() || Functions.IsPedArrested(subject))
                {
                    this.End();
                }
                if (this.pursuit != null && !Functions.IsPursuitStillRunning(this.pursuit))
                {
                    this.End();
                }
            }, "Suspicious Person Fiber [STREET CALLOUTS]");
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
