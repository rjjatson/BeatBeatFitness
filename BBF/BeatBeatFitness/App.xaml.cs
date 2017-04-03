using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Kinect.Xaml.Controls;
using Microsoft.Kinect.Toolkit.Input;
using System.Diagnostics;
using WindowsPreview.Kinect.Input;
using Windows.UI;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace BeatBeatFitness
{
    class ManualEngage : IKinectEngagementManager
    {
        public IReadOnlyList<BodyHandPair> KinectManualEngagedHands { get; set; }
        bool IKinectEngagementManager.EngagedBodyHandPairsChanged()
        {
            return false;
        }
        void IKinectEngagementManager.StartManaging()
        {

        }
        void IKinectEngagementManager.StopManaging()
        {

        }
    }
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        static KinectRegion kinectRegion;
        static KinectUserViewer kinectUserViewer;
        static ManualEngage manualEngagement = new ManualEngage();
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public static void ToggleKinectControl(bool isVisible)
        {
            if (isVisible)
            {
                kinectUserViewer.Visibility = Visibility.Visible;
                kinectRegion.SetKinectOnePersonSystemEngagement();
            }
            else
            {
                kinectUserViewer.Visibility = Visibility.Collapsed;
                kinectRegion.SetKinectOnePersonManualEngagement(manualEngagement);
                kinectRegion.ReleasePointerCaptures();
                kinectRegion.KinectEngagementManager.StopManaging();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }
                Grid kinectControlGrid = new Grid();
                kinectRegion = new KinectRegion();
                kinectUserViewer = new KinectUserViewer()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Width = 128,
                        Height = 106,
                        BorderThickness=new Thickness(3.0),
                        BorderBrush = new SolidColorBrush(Colors.Orange),
                        Margin = new Thickness(7.0),
                        
                    };
                kinectControlGrid.Children.Add(kinectRegion);
                kinectControlGrid.Children.Add(kinectUserViewer);

                kinectRegion.Content = rootFrame;
                // Place the frame in the current Window
                Window.Current.Content = kinectControlGrid;
                //Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(Menu), e.Arguments);
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
