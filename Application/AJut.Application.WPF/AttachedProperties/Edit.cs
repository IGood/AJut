﻿namespace AJut.Application.AttachedProperties
{
    using System;
    using System.Windows;

    public static class Edit
    {
        // =============[ Fields ]================
        private static readonly APUtilsRegistrationHelper APUtils = new APUtilsRegistrationHelper(typeof(Edit));

        /// <summary>
        /// A standardized way to specify readonly-ness in an inheritted high level. Set default control template/style for TextBox, Button, and other interactable
        /// elements to include this and then readonly can be specified from a high level!
        /// </summary>
        public static DependencyProperty IsReadOnlyProperty = APUtils.Register(GetIsReadOnly, SetIsReadOnly, new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnIsReadOnlyChanged));
        public static bool GetIsReadOnly (DependencyObject obj) => (bool)obj.GetValue(IsReadOnlyProperty);
        public static void SetIsReadOnly (DependencyObject obj, bool value) => obj.SetValue(IsReadOnlyProperty, value);
        private static void OnIsReadOnlyChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetIsEditable(d, !GetIsReadOnly(d));
        }

        private static DependencyPropertyKey IsEditablePropertyKey = APUtils.RegisterReadOnly(GetIsEditable, SetIsEditable);
        public static DependencyProperty IsEditableProperty = IsEditablePropertyKey.DependencyProperty;
        public static bool GetIsEditable (DependencyObject obj) => (bool)obj.GetValue(IsEditableProperty);
        internal static void SetIsEditable (DependencyObject obj, bool value) => obj.SetValue(IsEditablePropertyKey, value);
    }
}
