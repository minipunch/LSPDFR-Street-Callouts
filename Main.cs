using StreetCallouts.Callouts;
using LSPD_First_Response.Mod.API;
using Rage;

namespace StreetCallouts
{
    /// <summary>
    /// Do not rename! Attributes or inheritance based plugins will follow when the API is more in depth.
    /// </summary>
    public class Main : Plugin
    {
        /// <summary>
        /// Constructor for the main class, same as the class, do not rename.
        /// </summary>
        public Main()
        {
            
        }

        /// <summary>
        /// Called when the plugin ends or is terminated to cleanup
        /// </summary>
        public override void Finally()
        {

        }

        /// <summary>
        /// Called when the plugin is first loaded by LSPDFR
        /// </summary>
        public override void Initialize()
        {
            //Event handler for detecting if the player goes on duty
            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
            Game.LogTrivial("Street Callouts by minipunch loaded!");
            Game.DisplayNotification("~b~Street Callouts ~w~by ~b~minipunch ~w~has been loaded ~g~successfully!");
        }

        /// <summary>
        /// The event handler mentioned above,
        /// </summary>
        static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                //If the player goes on duty we need to register our custom callouts
                //Here we register our ExampleCallout class which is inside our Callouts folder (APIExample.Callouts namespace)
                //Functions.RegisterCallout(typeof(StolenVehicle));
                //Functions.RegisterCallout(typeof(SuspiciousPerson1));
                Functions.RegisterCallout(typeof(SuspiciousPerson2));
                //Functions.RegisterCallout(typeof(ManWithKnife));
                Game.LogTrivial("Street Callouts HAS BEEN LOADED");
            }
        }
    }
}
