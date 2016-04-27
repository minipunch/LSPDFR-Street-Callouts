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
    /*
    *
    *   Man with a knife, whom attacks you
    *
    */
    //Give your callout a string name and a probability of spawning. We also inherit from the Callout class, as this is a callout
    [CalloutInfo("ManWithKnife", CalloutProbability.Medium)]
    public class ManWithKnife : Callout
    {
        //Here we declare our variables, things we need or our callout
        private string[] pedList = new string[] { "A_F_M_SouCent_01", "A_F_M_SouCent_02", "A_M_Y_Skater_01", "A_M_M_FatLatin_01", "A_M_M_EastSA_01", "A_M_Y_Latino_01", "G_M_Y_FamDNF_01", "G_M_Y_FamCA_01", "G_M_Y_BallaSout_01", "G_M_Y_BallaOrig_01", "G_M_Y_BallaEast_01", "G_M_Y_StrPunk_02", "S_M_Y_Dealer_01", "A_M_M_RurMeth_01", "A_M_M_Skidrow_01", "A_M_Y_MexThug_01", "G_M_Y_MexGoon_03", "G_M_Y_MexGoon_02", "G_M_Y_MexGoon_01", "G_M_Y_SalvaGoon_01", "G_M_Y_SalvaGoon_02", "G_M_Y_SalvaGoon_03", "G_M_Y_Korean_01", "G_M_Y_Korean_02", "G_M_Y_StrPunk_01" };
        private string[] NotAcceptedResponses = new string[] { "OTHER_UNIT_TAKING_CALL_01", "OTHER_UNIT_TAKING_CALL_02", "OTHER_UNIT_TAKING_CALL_03", "OTHER_UNIT_TAKING_CALL_04", "OTHER_UNIT_TAKING_CALL_05", "OTHER_UNIT_TAKING_CALL_06", "OTHER_UNIT_TAKING_CALL_07" };
        private string[] CiviliansReporting = new string[] { "CITIZENS_REPORT_01", "CITIZENS_REPORT_02", "CITIZENS_REPORT_03", "CITIZENS_REPORT_04" };
        private string[] DispatchCopyThat = new string[] { "REPORT_RESPONSE_COPY_01", "REPORT_RESPONSE_COPY_02", "REPORT_RESPONSE_COPY_03", "REPORT_RESPONSE_COPY_04" };
        private string[] AConjunctive = new string[] { "A_01", "A_02" };
        private Ped subject;                    // our suspicious person
        private Vector3 SpawnPoint;             // area where suspicious person was spotted
        private Blip myBlip;                    // a gta v blip
        private LHandle pursuit;                // an API pursuit handle for any potential pursuits that occur
        private int callOutMessage = 0;
        private int scenario = 0;
        private Ped playerPed;
        private bool hasBegunAttacking = false;
        private bool msg1 = false;
        private bool msg2 = true;
        private bool isArmed = false;
        private int storyLine = 1;
        private Ped[] pedAr;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            playerPed = Game.LocalPlayer.Character;
            scenario = Common.myRand.Next(0, 100);

            //Set the spawn point of the crime to be on a street around 500f (distance) away from the player.
            SpawnPoint = World.GetNextPositionOnStreet(playerPed.Position.Around(520f));
            while (SpawnPoint.DistanceTo(playerPed.GetOffsetPosition(Vector3.RelativeFront)) < 200f)
            {
                SpawnPoint = World.GetNextPositionOnStreet(playerPed.Position.Around(520f));
            }

            subject = new Ped(pedList[Common.myRand.Next((int)pedList.Length)], SpawnPoint, 0f);

            subject.BlockPermanentEvents = true;
            subject.IsPersistent = true;
            NativeFunction.Natives.SetPedPathCanUseClimbovers(subject, true);


            // check for any errors
            if (!subject.Exists()) return false;

            // Show the user where the call out is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 100f);
            this.AddMinimumDistanceCheck(10f, subject.Position);

            // Set up our callout message and location
            switch (Common.myRand.Next(1, 2))
            {
                case 1:
                    this.CalloutMessage = "Subject with a knife.";
                    callOutMessage = 1;
                    break;
                case 2:
                    this.CalloutMessage = "Man armed with a knife";
                    callOutMessage = 2;
                    break;
            }
            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout
            Functions.PlayScannerAudioUsingPosition("ATTENTION_ALL_UNITS WE_HAVE_02 CIV_ASSISTANCE IN_OR_ON_POSITION", SpawnPoint);

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

            //tasks
            subject.Tasks.Wander();

            // let user know how to end call out
            Game.DisplayNotification("Press ~y~Ctrl ~w~+ ~y~Shft ~w~+ ~y~Y ~w~to end the call out at any time.");

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

            if (myBlip.Exists()) myBlip.Delete();
            if (subject.Exists()) subject.Delete();

            // have another unit "respond" to it
            Functions.PlayScannerAudio(this.NotAcceptedResponses[Common.myRand.Next((int)this.NotAcceptedResponses.Length)]);
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            GameFiber.StartNew(delegate
            {

                if (playerPed.IsDead) this.End();

                if (subject.DistanceTo(playerPed.GetOffsetPosition(Vector3.RelativeFront)) < 18f && !isArmed)
                {
                    // arm with a knife
                    NativeFunction.Natives.GiveWeaponToPed(subject, 0x99B507EA, 1, true, true);
                    isArmed = true;
                }

                // subject attacks player
                if (subject.Exists() && subject.DistanceTo(playerPed.GetOffsetPosition(Vector3.RelativeFront)) < 18f && !hasBegunAttacking)
                {
                   if(scenario > 40)
                   {
                        subject.Tasks.FightAgainst(playerPed);
                        hasBegunAttacking = true;
                        GameFiber.Wait(2000);
                    }
                    else
                    {
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, subject);
                        NativeFunction.Natives.TaskSwapWeapon(subject, true);
                    }

                   if(this.pursuit != null && !Functions.IsPursuitStillRunning(this.pursuit))
                    {
                        switch (Common.myRand.Next(1, 3))
                        {
                            case 1:
                                Game.DisplaySubtitle("~r~Suspect: ~w~Kill me, pig! Kill me!", 4000);
                                break;
                            case 2:
                                Game.DisplaySubtitle("~r~Suspect: ~w~Kill me! Come on, kill me!", 4000);
                                break;
                            case 3:
                                Game.DisplaySubtitle("~r~Suspect: ~w~Shoot me! Come on, shoot me!", 4000);
                                break;
                            default: break;
                        }
                    }

                }   

                if (subject.IsDead) this.End();

                if (Functions.IsPedArrested(subject)) this.End();

                if (!subject.Exists()) this.End();

                if (subject == null) this.End();

                if (pursuit != null && !Functions.IsPursuitStillRunning(pursuit))
                {
                    this.End();
                }

                // Press LCNTRL + LSHFT + Y to force end call out -- not working?
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                {
                    if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LShiftKey))
                    {
                        if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LControlKey))
                        {
                            Game.DisplaySubtitle("~b~You: ~w~Dispatch we're code 4. Show me ~g~10-8.", 4000);
                            Functions.PlayScannerAudio(this.DispatchCopyThat[Common.myRand.Next((int)DispatchCopyThat.Length)]);
                            this.End();
                        }
                    }
                }

            }, "Man with a knife [STREET CALLOUTS]");
        }

        /// <summary>
        /// More cleanup, when we call 
        /// end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if (subject.Exists()) subject.Dismiss();
            if (myBlip.Exists()) myBlip.Delete();
            base.End();

        }
    }
}
