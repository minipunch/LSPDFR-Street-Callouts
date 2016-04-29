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
    // Some naughty people doing drugs, or something naughty like that.
    //Give your callout a string name and a probability of spawning. We also inherit from the Callout class, as this is a callout
    [CalloutInfo("SuspiciousPerson2", CalloutProbability.VeryHigh)]
    public class SuspiciousPerson2 : Callout
    {
        //Here we declare our variables, things we need or our callout
        private string[] pedList = new string[] { "A_F_M_SouCent_01", "A_F_M_SouCent_02", "A_M_Y_Skater_01", "A_M_M_FatLatin_01", "A_M_M_EastSA_01", "A_M_Y_Latino_01", "G_M_Y_FamDNF_01", "G_M_Y_FamCA_01", "G_M_Y_BallaSout_01", "G_M_Y_BallaOrig_01", "G_M_Y_BallaEast_01", "G_M_Y_StrPunk_02", "S_M_Y_Dealer_01", "A_M_M_RurMeth_01", "A_M_M_Skidrow_01", "A_M_Y_MexThug_01", "G_M_Y_MexGoon_03", "G_M_Y_MexGoon_02", "G_M_Y_MexGoon_01", "G_M_Y_SalvaGoon_01", "G_M_Y_SalvaGoon_02", "G_M_Y_SalvaGoon_03", "G_M_Y_Korean_01", "G_M_Y_Korean_02", "G_M_Y_StrPunk_01" };
        private string[] NotAcceptedResponses = new string[] { "OTHER_UNIT_TAKING_CALL_01", "OTHER_UNIT_TAKING_CALL_02", "OTHER_UNIT_TAKING_CALL_03", "OTHER_UNIT_TAKING_CALL_04", "OTHER_UNIT_TAKING_CALL_05", "OTHER_UNIT_TAKING_CALL_06", "OTHER_UNIT_TAKING_CALL_07" };
        private string[] CiviliansReporting = new string[] { "CITIZENS_REPORT_01", "CITIZENS_REPORT_02", "CITIZENS_REPORT_03", "CITIZENS_REPORT_04" };
        private string[] DispatchCopyThat = new string[] { "REPORT_RESPONSE_COPY_01", "REPORT_RESPONSE_COPY_02", "REPORT_RESPONSE_COPY_03", "REPORT_RESPONSE_COPY_04" };
        private string[] AConjunctive = new string[] { "A_01", "A_02" };
        private Ped subject1;                    // our suspicious person
        private Ped subject2;                    // our suspicious person
        private Vector3 SpawnPoint;             // area where suspicious person was spotted
        private Blip myBlip;                    // a gta v blip
        private Blip myBlip2;
        private LHandle pursuit;                // an API pursuit handle for any potential pursuits that occur
        private LHandle pursuit2;
        private Ped playerPed;
        private int callOutMessage = 0;
        private int scenario = 0;
        private bool firstPedSmoking = false;
        private bool secondPedSmoking = false;
        private bool hasPursuitStarted = false;
        private bool hasPursuitStarted2 = false;
        private string pedModelName1;
        private string pedModelName2;
        private Rage.Object joint1;
        private Rage.Object joint2;
        private Vector3 pedCoords1;
        private Vector3 pedCoords2;
        private int boneIndex = 0;
        private Rage.Object droppedItem1;
        private Rage.Object droppedItem2;
        private Blip droppedItemBlip1;
        private Blip droppedItemBlip2;
        private bool hasDroppedItem = false;
        private bool hasDroppedItem2 = false;
        private Rage.Object blipHelper1;
        private Rage.Object blipHelper2;
        private int droppedItemCount = 0;
        private int pickedUpItemCount = 0;
        private bool hasIntrod = false;
        private int storyline1 = 1;
        private bool playerHasDied = false;
        private bool isOnPhone1 = false;
        private bool isOnPhone2 = false;

        /// <summary>
        /// OnBeforeCalloutDisplayed is where we create a blip for the user to see where the pursuit is happening, we initiliaize any variables above and set
        /// the callout message and position for the API to display
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            scenario = Common.myRand.Next(0, 100);
            playerPed = Game.LocalPlayer.Character;

            //Set the spawn point of the crime to be on a street around 500f (distance) away from the player.
            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(625f));
            while (SpawnPoint.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 370f)
            {
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(625f));
            }

            try
            {
                pedModelName1 = pedList[Common.myRand.Next((int)pedList.Length)];
                pedModelName2 = pedList[Common.myRand.Next((int)pedList.Length)];
                // to see what models are invalid in my pedList:
                Game.LogTrivial("SPAWNED MODEL NAMES: " + pedModelName1 + " / " + pedModelName2);
                subject1 = new Ped(pedModelName1, SpawnPoint, 0f);
                subject2 = new Ped(pedModelName2, SpawnPoint, 5f);
                pedCoords1 = subject1.Position;
                pedCoords2 = subject2.Position;
            }
            catch (Exception e)
            {
                Game.LogTrivial("STREETCALLOUTS ERROR: " + e.Message);
                Game.LogVerboseDebug("STACK TRACE: " + e.StackTrace);
            };

            // check for any errors
            if (!subject1.Exists()) return false;
            if (!subject2.Exists()) return false;

            subject1.BlockPermanentEvents = true;
            subject2.BlockPermanentEvents = true;
            subject1.IsPersistent = true;
            subject2.IsPersistent = true;
            NativeFunction.Natives.SetPedPathCanUseClimbovers(subject1, true);
            NativeFunction.Natives.SetPedPathCanUseClimbovers(subject2, true);

            // Show the user where the call out is about to happen and block very close peds.
            this.ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 100f);
            this.AddMinimumDistanceCheck(10f, subject1.Position);
            this.AddMinimumDistanceCheck(10f, subject2.Position);

            // Set up our callout message and location
            if (Common.myRand.Next(0, 100) < 50)
            {
                this.CalloutMessage = "Suspicious Persons.\nPossible drug use.";
                callOutMessage = 1;
            }
            else
            {
                this.CalloutMessage = "Suspicious Party.\nPossible 415.";
                callOutMessage = 2;
            }

            this.CalloutPosition = SpawnPoint;

            //Play the police scanner audio for this callout
            Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 POSSIBLE_DISTURBANCE IN_OR_ON_POSITION", SpawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }


        /// <summary>
        /// OnCalloutAccepted is where we begin our callout's logic. In this instance we create our pursuit and add our ped from eariler to the pursuit as well
        /// </summary>
        /// <returns></returns>
        public override bool OnCalloutAccepted()
        {
            try
            {
                //We accepted the callout, so lets initilize our blip from before and attach it to our perp(s) so we know where he is.
                myBlip = subject1.AttachBlip();
                myBlip.Color = Color.Yellow;
                myBlip2 = subject2.AttachBlip();
                myBlip2.Color = Color.Yellow;
            }
            catch (Exception e)
            {
                Game.LogTrivial("STREETCALLOUTS ERROR: " + e.Message);
                Game.LogVerboseDebug("STACK TRACE: " + e.StackTrace);
            };

            if (!myBlip.Exists()) return false;
            if (!myBlip2.Exists()) return false;

            GameFiber.StartNew(delegate
            {
                // max distance check for the two subjects so they don't separate
                if (subject1.DistanceTo(subject2) > 10f)
                {
                    subject1.Tasks.GoToOffsetFromEntity(subject2, 7f, 1f, 2.0f);
                }

                if (subject2.DistanceTo(subject1) > 10f)
                {
                    subject2.Tasks.GoToOffsetFromEntity(subject1, 7f, 1f, 2.0f);
                }

                // stand still
                if (subject1.Exists()) subject1.Velocity = Vector3.Zero;
                if (subject2.Exists()) subject2.Velocity = Vector3.Zero;

                // 40% chance for first ped to be smoking
                if (Common.myRand.Next(0, 100) < 40)
                {
                    joint1 = new Rage.Object("prop_sh_joint_01", pedCoords1);
                    boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(subject1, (int)PedBoneId.RightIndexFinger1);
                    NativeFunction.CallByName<int>("ATTACH_ENTITY_TO_ENTITY", joint1, subject1, boneIndex, 0f, 0f, 0f, subject1.Rotation.Pitch, subject1.Rotation.Yaw, subject1.Rotation.Roll, true, false, false, false, 2, 1);
                    subject1.Tasks.PlayAnimation("timetable@gardener@smoking_joint", "smoke_idle", 2, AnimationFlags.Loop);
                    firstPedSmoking = true;
                }

                GameFiber.Wait(1000);

                // 35% chance for second ped to be smoking
                if (scenario < 35)
                {
                    joint2 = new Rage.Object("prop_sh_joint_01", pedCoords2);
                    boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(subject2, (int)PedBoneId.RightIndexFinger1);
                    NativeFunction.CallByName<int>("ATTACH_ENTITY_TO_ENTITY", joint2, subject2, boneIndex, 0f, 0f, 0f, subject2.Rotation.Pitch, subject2.Rotation.Yaw, subject2.Rotation.Roll, true, false, false, false, 2, 1);
                    subject2.Tasks.PlayAnimation("timetable@gardener@smoking_joint", "smoke_idle", 2, AnimationFlags.Loop);
                    secondPedSmoking = true;
                }

            }, "Dope Smokers Fiber 1 [STREET CALLOUTS]");


            Functions.PlayScannerAudio(this.DispatchCopyThat[Common.myRand.Next((int)this.DispatchCopyThat.Length)]);
            Game.DisplaySubtitle("Make contact with the ~r~subjects.", 6500);
            GameFiber.Wait(2000);
            // let user know how to end call out
            Game.DisplayHelp("Press ~y~Ctrl ~w~+ ~y~Shft ~w~+ ~y~Y ~w~to end the call out at any time.");

            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// If you don't accept the callout this will be called, we clear anything we spawned here to prevent it staying in the game
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            if (myBlip.Exists()) myBlip.Delete();
            if (myBlip2.Exists()) myBlip2.Delete();
            if (subject1.Exists()) subject1.Delete();
            if (subject2.Exists()) subject2.Delete();
            if (joint1.Exists()) joint1.Delete();
            if (joint2.Exists()) joint2.Delete();

            // have another unit "respond" to it
            Functions.PlayScannerAudio(this.NotAcceptedResponses[Common.myRand.Next((int)this.NotAcceptedResponses.Length)]);
        }

        //This is where it all happens, run all of your callouts logic here
        public override void Process()
        {
            base.Process();

            if(playerPed.Exists())
            {
                if(playerPed.IsDead)
                {
                    playerHasDied = true;
                }
            }

            // Subject 1 dialogue
            if (subject1.Exists() && subject1.IsAlive && !hasPursuitStarted && !hasPursuitStarted2)
            {
                if (!firstPedSmoking && playerPed.DistanceTo(subject1) < 15f)
                {
                    if(!hasIntrod)
                    {
                        Game.DisplayHelp("Press ~y~Y ~w~to speak with the subject(s).");
                        hasIntrod = true;
                    }

                    if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        if(!secondPedSmoking)
                        {
                            NativeFunction.Natives.TaskTurnPedToFaceEntity(subject1, playerPed, 4500);

                            switch (storyline1)
                            {
                                case 1:
                                    Game.DisplaySubtitle("~y~Suspect: ~w~Hello officer! What can I do you for?", 4000);
                                    storyline1++;
                                    break;
                                case 2:
                                    Game.DisplaySubtitle("~b~You: ~w~What are you guys doing?", 4000);
                                    storyline1++;
                                    break;
                                case 3:
                                    Game.DisplaySubtitle("~y~Suspect: ~w~We're just hanging out waiting for a friend to come pick us up for a concert!", 4000);
                                    storyline1++;
                                    break;
                                case 4:
                                    Game.DisplaySubtitle("~b~You: ~w~You guys wouldn't have any illegal substances or anything like that would you now?", 4000);
                                    storyline1++;
                                    break;
                                case 5:
                                    Game.DisplaySubtitle("~y~Suspect: ~w~Heck no! I just got out of prison, sir! I can't be messing with that stuff any mo'!", 4000);
                                    storyline1++;
                                    break;
                            }
                        }
                    }
                }
            }

            if (firstPedSmoking && !hasPursuitStarted)
            {
                if (playerPed.DistanceTo(subject1) < 23f && Common.myRand.Next(0, 100) < 40)
                {
                    GameFiber.Wait(2100);
                    this.pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, subject1);
                    hasPursuitStarted = true;
                    firstPedSmoking = false;
                    // to make subject drop the joint when running
                    joint1.Delete();
                    joint1 = new Rage.Object("prop_sh_joint_01", subject1.Position.Around(2f));
                    Game.DisplayNotification("Press the ~y~Insert ~w~key to mark evidence on the minimap!");
                }
            }

            if (secondPedSmoking && !hasPursuitStarted2)
            {
                if (playerPed.DistanceTo(subject2) < 23f && Common.myRand.Next(0, 100) < 40)
                {
                    GameFiber.Wait(2100);
                    this.pursuit2 = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit2, subject2);
                    hasPursuitStarted2 = true;
                    secondPedSmoking = false;
                    // to make subject drop the joint when running
                    joint2.Delete();
                    joint2 = new Rage.Object("prop_sh_joint_01", subject2.Position.Around(2f));
                }
            }

            // drop a blip on any evidence dropped by subjects with 'Insert' key
            if (droppedItem1.Exists() || droppedItem2.Exists())
            {
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Insert))
                {
                    if (droppedItem1.Exists())
                    {
                        if (playerPed.DistanceTo(droppedItem1) < 30f && !droppedItemBlip1.Exists())
                        {
                            blipHelper1 = new Rage.Object("prop_sh_joint_01", playerPed.Position);
                            NativeFunction.Natives.SetEntityVisible(blipHelper1, false, false);
                            droppedItemBlip1 = blipHelper1.AttachBlip();
                            droppedItemBlip1.Color = Color.ForestGreen;
                            Game.DisplayNotification("Marker placed.");
                        }
                    }
                    if (droppedItem2.Exists())
                    {
                        if (playerPed.DistanceTo(droppedItem2) < 30f && !droppedItemBlip2.Exists())
                        {
                            blipHelper2 = new Rage.Object("prop_sh_joint_01", playerPed.Position);
                            NativeFunction.Natives.SetEntityVisible(blipHelper2, false, false);
                            droppedItemBlip2 = blipHelper2.AttachBlip();
                            droppedItemBlip2.Color = Color.ForestGreen;
                            Game.DisplayNotification("Marker placed.");
                        }
                    }
                }
            }

            // control dropping of evidence
            if (!droppedItem1.Exists() && !hasDroppedItem && pursuit != null && hasPursuitStarted)
            {
                dropWhenFleeing1();
                GameFiber.Wait(1500);
            }
            //GameFiber.Wait(1500);
            // control dropping of evidence
            if (!droppedItem2.Exists() && !hasDroppedItem2 && pursuit2 != null && hasPursuitStarted2)
            {
                dropWhenFleeing2();
                GameFiber.Wait(1500);
            }
            //GameFiber.Wait(1500);
            // control picking up of evidence
            if ((hasDroppedItem && pursuit != null && !Functions.IsPursuitStillRunning(pursuit)) || (hasDroppedItem2 && pursuit2 != null && !Functions.IsPursuitStillRunning(pursuit2)))
            {
                beginBacktrack();
            }

            if(subject1.Exists())
                if (subject1.IsDead) this.End();

            if (!subject1.Exists()) this.End();

            if (subject1 == null) this.End();

            if(subject2.Exists())
                if (subject2.IsDead) this.End();

            //if (Functions.IsPedArrested(subject2)) this.End();

            if (!subject2.Exists()) this.End();

            if (subject2 == null) this.End();

            if(playerPed.Exists())
            {
                if (playerPed.IsDead) this.End();
            }

            // Press LCNTRL + LSHFT + Y to force end call out
            if (Game.IsKeyDown(System.Windows.Forms.Keys.Y))
            {
                if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LShiftKey))
                {
                    if (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.LControlKey))
                    {
                        Game.DisplaySubtitle("~b~You: ~w~Dispatch we're ~g~CODE 4~w~. Show me 10-8.", 4000);
                        Functions.PlayScannerAudio(this.DispatchCopyThat[Common.myRand.Next((int)DispatchCopyThat.Length)]);
                        this.End();
                    }
                }
            }
        }

        /// <summary>
        /// More cleanup, when we call 
        /// end you clean away anything left over
        /// This is also important as this will be called if a callout gets aborted (for example if you force a new callout)
        /// </summary>
        public override void End()
        {
            if(droppedItemCount != 0 && !playerHasDied)
                Game.DisplayNotification("~g~" + pickedUpItemCount + " ~w~of ~g~" + droppedItemCount + " ~w~peices of evidence recovered. Nice job!");
            else if(droppedItemCount == 0 && (hasPursuitStarted || hasPursuitStarted2))
                Game.DisplayNotification("~r~" + pickedUpItemCount + " ~w~of ~g~" + droppedItemCount + " ~w~peices of evidence recovered. Better luck next time!");

            if (myBlip.Exists()) myBlip.Delete();
            if (myBlip2.Exists()) myBlip2.Delete();
            if (joint1.Exists()) joint1.Delete();
            if (joint2.Exists()) joint2.Delete();
            if (droppedItem1.Exists()) droppedItem1.Delete();
            if (droppedItem2.Exists()) droppedItem2.Delete();
            if (droppedItemBlip1.Exists()) droppedItemBlip1.Delete();
            if (droppedItemBlip2.Exists()) droppedItemBlip1.Delete();
            if (subject1.Exists())
            {
                if (!Functions.IsPedArrested(subject1))
                    subject1.Tasks.Wander();
            }
            if(subject2.Exists())
            {
                if (!Functions.IsPedArrested(subject2))
                    subject2.Tasks.Wander();
            }
            
            if (subject1.Exists()) subject1.Dismiss();
            if (subject2.Exists()) subject2.Dismiss();
            base.End();

        }

        // handles the dropping of items during a pursuit for subject1
        public void dropWhenFleeing1()
        {
            // wait a little bit before making suspect drop evidence
            GameFiber.Wait(Common.myRand.Next(6, 14) * 1000);

            if (!hasDroppedItem && !droppedItem1.Exists())
            {
                droppedItem1 = new Rage.Object("prop_mp_drug_package", subject1.Position.Around(2f));
                droppedItemCount++;
                hasDroppedItem = true;
            }
            //some potential item models: 
            //prop_meth_bag_01
            //prop_mp_drug_package
            //prop_grass_dry_02
            //prop_hacky_sack_01
            //prop_knife
            //prop_ld_case_01
        }

        // handles the dropping of items during a pursuit for subject2
        public void dropWhenFleeing2()
        {
            // wait a little bit before making suspect drop evidence
            GameFiber.Wait(Common.myRand.Next(6, 14) * 1000);

            if (!hasDroppedItem2 && !droppedItem2.Exists())
            {
                droppedItem2 = new Rage.Object("prop_mp_drug_package", subject2.Position.Around(2f));
                droppedItemCount++;
                hasDroppedItem2 = true;
            }
        }

        /// <summary>
        /// Handles picking up of any dropped evidence
        /// </summary>
        public void beginBacktrack()
        {
            if (droppedItem1.Exists())
            {
                if (playerPed.DistanceTo(droppedItem1) < 2f && !isAnyPursuitStillRunning())
                {
                    droppedItem1.Delete();
                    droppedItemBlip1.Delete();
                    pickedUpItemCount++;
                    Game.DisplayNotification("You picked up ~r~3.6 grams of Marijuana.");
                }
            }

            if (droppedItem2.Exists())
            {
                if (playerPed.DistanceTo(droppedItem2) < 2f && !isAnyPursuitStillRunning())
                {
                    droppedItem2.Delete();
                    droppedItemBlip2.Delete();
                    pickedUpItemCount++;
                    Game.DisplayNotification("You picked up ~r~3.6 grams of Marijuana.");
                }
            }
        }

        public bool isAnyPursuitStillRunning()
        {
            if (this.pursuit != null && Functions.IsPursuitStillRunning(this.pursuit))
                return true;

            if (this.pursuit2 != null && Functions.IsPursuitStillRunning(this.pursuit2))
                return true;

            return false;
        }

    }
}
