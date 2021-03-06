﻿using Microsoft.Graphics.Canvas.Effects;
using PRPR.Common;
using PRPR.BooruViewer.Models;
using PRPR.BooruViewer.Models.Global;
using PRPR.BooruViewer.Services;
using PRPR.BooruViewer.ViewModels;
using PRPR.BooruViewer.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Effects;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Windows.ApplicationModel;
using PRPR.Common.Views.Controls;
using PRPR.Common.Models;
using System.Collections;
using PRPR.Common.Services;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.Resources;
using Microsoft.QueryStringDotNET;
using PRPR.Common.Controls;
using Windows.Foundation.Metadata;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PRPR.BooruViewer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        #region NavigationHelper

        private NavigationHelper navigationHelper;
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        
        public HomeViewModel HomeViewModel
        {
            get
            {
                return this.DataContext as HomeViewModel;
            }
        }


        
        public string AppVersion
        {
            get
            {
                var v = Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision);
            }
        }
        
        


        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            bool ResumingExistingPage = e.PageState != null && e.PageState.ContainsKey("Tags");

            
            if (ResumingExistingPage)
            {
                // Re-search the tags if needed
                if (SearchBox.Text != e.PageState["Tags"] as string)
                {
                    SearchBox.Text = e.PageState["Tags"] as string;
                    FlipView.SelectedIndex = (int) (e.PageState["Tab"]);
                    try
                    {
                        this.HomeViewModel.Posts = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags={WebUtility.UrlEncode(SearchBox.Text)}");

                    }
                    catch (Exception ex)
                    {
                        this.HomeViewModel.Posts = new Posts();
                    }

                    if (HomeViewModel.BrowsePosts == null)
                    {
                        var s = new ImageWallRows<Post>()
                        {
                            ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter)
                        };
                        HomeViewModel.BrowsePosts = s;
                        BrowsePanel.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);

                    }
                    else
                    {
                        HomeViewModel.BrowsePosts.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);
                        BrowsePanel.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);

                    }
                }
            }
            else // Newly entered a page
            {

                if (!String.IsNullOrEmpty(e.NavigationParameter as string))
                {
                    // Turn to the searching selection
                    this.HomeViewModel.SelectedViewIndex = 1;
                }

                SearchBox.Text = e.NavigationParameter as string;
                try
                {
                    this.HomeViewModel.Posts = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags={WebUtility.UrlEncode(SearchBox.Text)}");
                }
                catch (Exception ex)
                {
                    this.HomeViewModel.Posts = new Posts();
                }

                var s = new ImageWallRows<Post>()
                {
                    ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter)
                };
                HomeViewModel.BrowsePosts = s;
                BrowsePanel.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);


                // Load the favorites if logged in
                if (YandeSettings.Current.IsLoggedIn)
                {
                    Posts favoritePost = new Posts();
                    try
                    {
                        favoritePost = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags=vote:3:{YandeSettings.Current.UserName}+order:vote");
                    }
                    catch (Exception ex)
                    {

                    }
                    FavoritePanel.ItemsSource = new FilteredCollection<Post, Posts>(favoritePost, this.HomeViewModel.SearchPostFilter);
                }
            }
        }
        
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            e.PageState["Tags"] = SearchBox.Text;
            e.PageState["Tab"] = FlipView.SelectedIndex;
        }

        private void ScrollingHost_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            
        }





        private void BrowseGridViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Clicked a list item from the image wall

            

            var container = (sender as GridViewItem);
            if (container != null)
            {
                var root = (FrameworkElement)container.ContentTemplateRoot;
                var image = (UIElement)root.FindName("PreviewImage");

                // Pre-fall creator has different image loading order
                // unable to share same connected animation code without breaking the UI
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PreviewImage", image);
                }
            }

            // Add a fade out effect
            Transitions = new TransitionCollection();
            Transitions.Add(new ContentThemeTransition());


            var post = (sender as GridViewItem).DataContext as Post;
            //this.Frame.Navigate(typeof(ImagePage), post.ToXml(), new SuppressNavigationTransitionInfo());

           
            
            // Navigate to image page
            App.Current.Resources["Posts"] = (FilteredCollection<Post, Posts>)BrowsePanel.ItemsSource;
            this.Frame.Navigate(typeof(ImagePage), post.ToXml(), new SuppressNavigationTransitionInfo());

        }


        private void FavoriteGridViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Clicked a list item from the image wall



            var container = (sender as GridViewItem);
            if (container != null)
            {
                var root = (FrameworkElement)container.ContentTemplateRoot;
                var image = (UIElement)root.FindName("PreviewImage");

                // Pre-fall creator has different image loading order
                // unable to share same connected animation code without breaking the UI
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PreviewImage", image);
                }
            }

            // Add a fade out effect
            Transitions = new TransitionCollection();
            Transitions.Add(new ContentThemeTransition());


            var post = (sender as GridViewItem).DataContext as Post;
            //this.Frame.Navigate(typeof(ImagePage), post.ToXml(), new SuppressNavigationTransitionInfo());



            // Navigate to image page
            App.Current.Resources["Posts"] = (FilteredCollection<Post, Posts>)FavoritePanel.ItemsSource;
            this.Frame.Navigate(typeof(ImagePage), post.ToXml(), new SuppressNavigationTransitionInfo());
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            try
            {
                // Jump to the page item if this is a back button action
                if (this.Frame.CanGoForward)
                {
                    var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
                    var lastPageParameters = frameState["Page-" + (this.Frame.BackStackDepth + 1)] as IDictionary<string, object>;
                    var index = (int)lastPageParameters["Index"];
                    var postId = (int)lastPageParameters["PostId"];
                    

                    if (this.HomeViewModel.SelectedViewIndex == 0)
                    {

                        // Pre-fall creator has different image loading order
                        // unable to share same connected animation code without breaking the UI
                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                        {

                            var post = FeatureView.FeatureViewModel.TopToday.First(o => o.Id == postId);
                            if (post != null && FeatureView.FeatureViewModel.TopToday.IndexOf(post) != -1)
                            {
                                // Start the animation
                                ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PreviewImage");
                                if (animation != null)
                                {
                                    FeatureView.UpdateLayout();
                                    animation.TryStart(this.FeatureView.GetTopTodayButton(FeatureView.FeatureViewModel.TopToday.IndexOf(post)));
                                }
                            }

                        }
                    }
                    else if(this.HomeViewModel.SelectedViewIndex == 1 || this.HomeViewModel.SelectedViewIndex == 2)
                    {
                        JustifiedWrapPanel panel = null;
                        if (this.HomeViewModel.SelectedViewIndex == 1)
                        {
                            // Navigating back from a search result image
                            panel = BrowsePanel;
                        }
                        else if (this.HomeViewModel.SelectedViewIndex == 2)
                        {
                            // Navigating back from a favorite image
                            panel = FavoritePanel;
                        }

                        // Scroll into the index of last opened page
                        panel.ScrollIntoView((panel.ItemsSource as IList)[index], ScrollIntoViewAlignment.Default);
                        panel.UpdateLayout();




                        // Pre-fall creator has different image loading order
                        // unable to share same connected animation code without breaking the UI
                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                        {
                            // Start the animation
                            ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PreviewImage");
                            if (animation != null)
                            {
                                if (panel.ContainerFromIndex(index) is ContentControl container)
                                {
                                    var root = (FrameworkElement)container.ContentTemplateRoot;
                                    var image = (UIElement)root.FindName("PreviewImage");
                                    animation.TryStart(image);
                                }
                            }
                        }



                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        
        


        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterMainFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }
        
        private void MenuFlyoutSubItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterRatingFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }

        private void FilterReturnItem_Tapped(object sender, TappedRoutedEventArgs e)
        {

            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterMainFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }
        
        private async void FavoriteRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (YandeSettings.Current.UserName != "")
            {
                Posts favoritePost = null;
                try
                {
                    favoritePost = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags=vote:3:{YandeSettings.Current.UserName}+order:vote");

                }
                catch (Exception ex)
                {
                    return;
                }
                FavoritePanel.ItemsSource = new FilteredCollection<Post, Posts>(favoritePost, this.HomeViewModel.SearchPostFilter);
            }
        }
        



        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await FeatureView.FeatureViewModel.Update();
            }
            catch (Exception ex)
            {
                
            }

        }

        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterRatioFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }
        

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (HomeViewModel.BrowsePosts == null)
            {
                var s = new ImageWallRows<Post>();
                
                try
                {
                    this.HomeViewModel.Posts = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags={WebUtility.UrlEncode(SearchBox.Text)}");
                }
                catch (Exception ex)
                {

                }
                s.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);
                HomeViewModel.BrowsePosts = s;
                BrowsePanel.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);
            }
            else
            {

                try
                {
                    this.HomeViewModel.Posts = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags={WebUtility.UrlEncode(SearchBox.Text)}");
                }
                catch (Exception ex)
                {

                }
                HomeViewModel.BrowsePosts.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);
                BrowsePanel.ItemsSource = new FilteredCollection<Post, Posts>(this.HomeViewModel.Posts, this.HomeViewModel.SearchPostFilter);
            }

        }




        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = VisualTreeHelper.GetChild(sender as AutoSuggestBox, 0) as Grid;
            var textbox = grid.Children.First() as TextBox;
            textbox.SelectionChanged += Textbox_SelectionChanged;
        }


        private int lastSelectionStart = 0;

        private void Textbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Re-search suggestions if the user move the cursor position to another word
            // Except the cursor is at the end, bcs it is probably caused by a SuggestionChosen event
            var newSelectionStart = (sender as TextBox).SelectionStart;
            int newSelecetedKeyIndex = (sender as TextBox).Text.Take(newSelectionStart).Count(o => o == ' ');
            int lastSelecetedKeyIndex = (sender as TextBox).Text.Take(lastSelectionStart).Count(o => o == ' ');
            if (lastSelecetedKeyIndex != newSelecetedKeyIndex && newSelectionStart != (sender as TextBox).Text.Length)
            {
                UpdateSuggestion(SearchBox);
            }
            lastSelectionStart = newSelectionStart;
        }

        private void SearchBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var grid = VisualTreeHelper.GetChild(sender as AutoSuggestBox, 0) as Grid;
            var textbox = grid.Children.First() as TextBox;
            textbox.SelectionChanged -= Textbox_SelectionChanged;
        }

        private void UpdateSuggestion(AutoSuggestBox sender)
        {
            try
            {
                var grid = VisualTreeHelper.GetChild(sender, 0) as Grid;
                var textbox = grid.Children.First() as TextBox;
                var pointer = textbox.SelectionStart;
                int selecetedKeyIndex = sender.Text.Take(pointer).Count(o => o == ' ');

                var tags = sender.Text.Split(' ');
                if (tags.Length >= 1 && tags[selecetedKeyIndex] != "")
                {
                    var results = TagDataBase.Search(tags[selecetedKeyIndex]);

                    // Add back the other tags to the string
                    var prefix = String.Join(" ", tags.Take(selecetedKeyIndex));
                    if (!String.IsNullOrWhiteSpace(prefix))
                    {
                        prefix += " ";
                    }
                    var suffix = String.Join(" ", tags.Skip(selecetedKeyIndex + 1));
                    if (!String.IsNullOrWhiteSpace(suffix))
                    {
                        suffix = " " + suffix;
                    }

                    // Also add an extra space at the end so that its easier to start typing next new type
                    var results2 = results.Select(o => new TagDetailInMiddle(o, prefix, suffix + " "));

                    // Display the tag search results
                    sender.ItemsSource = results2;
                }
                else
                {
                    sender.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //await Task.Delay(200);
                if (args.CheckCurrent())
                {
                    UpdateSuggestion(sender);
                }
            }
        }

        private void FilterHiddenPostsListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterHiddenFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }



        private void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            var i = sender as Image;

            var b = i.Parent as Border;
            if (b == null)
            {
                i.Opacity = 1;
                return;
            }

            var g = b.Parent as Grid;
            if (g == null)
            {
                i.Opacity = 1;
                return;
            }


            var c = g.Parent as UserControl;
            if (c == null)
            {
                i.Opacity = 1;
                return;
            }

            VisualStateManager.GoToState(c, "ImageLoaded", true);
        }




        public ObservableCollection<string> UpdateNotes
        {
            get
            {
                var loader = ResourceLoader.GetForCurrentView();
                var notes = loader.GetString("/PatchNotes/Notes").Split('@').Where(o => !String.IsNullOrEmpty(o));

                return new ObservableCollection<string>(notes);
            }
        }

    }
}
