﻿using PRPR.BooruViewer.Models;
using PRPR.BooruViewer.Models.Global;
using PRPR.BooruViewer.Services;
using PRPR.BooruViewer.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PRPR.BooruViewer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingLockscreenPage : Page
    {
        public SettingLockscreenPage()
        {
            this.InitializeComponent();
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            await LockscreenUpdateTask.RunAsync();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Check the best time span to the users
            var p = await Posts.DownloadPostsAsync(1, $"https://yande.re/post.xml?tags={ WebUtility.UrlEncode(YandeSettings.Current.LockscreenUpdateTaskSearchKey) }");

            while (p.Count < 50 && p.HasMoreItems)
            {
                await p.LoadMoreItemsAsync(1);
            }

            List<int> timeSpans = new List<int>();

            if (p.Count < 2)
            {
                await new MessageDialog($"There are not much results for this keyword. Please try some other keys.", $"Result for \"{YandeSettings.Current.LockscreenUpdateTaskSearchKey}\"").ShowAsync();

            }
            else
            {
                for (int i = 0; i < Math.Min(p.Count - 1, 49); i++)
                {
                    var timeSpan = p[i].CreatedAtUtc.Subtract(p[i + 1].CreatedAtUtc);
                    timeSpans.Add((int)Math.Round(timeSpan.TotalMinutes));
                }
                timeSpans = timeSpans.OrderByDescending(o => o).ToList();
                var settingTime = YandeSettings.Current.LockscreenUpdateTaskTimeSpan;
                await new MessageDialog(
$"You have 90% chance to get a new image for {Search(0.90, timeSpans)} minutes.\nYou have 75% chance to get a new image for {Search(0.75, timeSpans)} minutes.\nYou have 50% chance to get a new image for {Search(0.50, timeSpans)} minutes.\nYou have 25% chance to get a new image for {Search(0.25, timeSpans)} minutes.", $"Result for \"{YandeSettings.Current.LockscreenUpdateTaskSearchKey}\"").ShowAsync();
            }

        }


        uint Search(double preferChance, List<int> timeSpans)
        {

            uint lastAttempt = 0;
            uint currentAttempt = 240;

            while (CheckChance(currentAttempt, timeSpans) < preferChance)
            {
                currentAttempt *= 2;
            }

            while (currentAttempt != lastAttempt)
            {
                var gap = Math.Max(lastAttempt, currentAttempt) - Math.Min(lastAttempt, currentAttempt);

                var result = CheckChance(currentAttempt, timeSpans);
                if (result == preferChance)
                {
                    return currentAttempt;
                }
                else if (result > preferChance) // Too long time span
                {
                    lastAttempt = currentAttempt;
                    currentAttempt -= gap / 2;
                }
                else // Too short time span
                {
                    lastAttempt = currentAttempt;
                    currentAttempt += gap / 2;
                }


            }

            return currentAttempt;
        }



        double CheckChance(uint timeSpan, List<int> timeSpans)
        {
            return 1.0 * (timeSpans.Sum(o => Math.Min(timeSpan, o)) + timeSpan) / (timeSpans.Sum() + timeSpan);

        }



        private void FilterReturnItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterMainFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterMainFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }

        private void ListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterRatioFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }

        private void MenuFlyoutSubItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterRatingFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }

        private void FilterHiddenPostsListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout.SetAttachedFlyout(FilterButton, this.Resources["FilterHiddenFlyout"] as Flyout);
            Flyout.ShowAttachedFlyout(FilterButton);
        }
    }
}
