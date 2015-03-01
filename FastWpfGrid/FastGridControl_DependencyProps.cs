using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        #region property Model

        public IFastGridModel Model
        {
            get { return (IFastGridModel) this.GetValue(ModelProperty); }
            set { this.SetValue(ModelProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
            "Model", typeof (IFastGridModel), typeof (FastGridControl), new PropertyMetadata(null, OnModelPropertyChanged));

        private static void OnModelPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((FastGridControl) dependencyObject).OnModelPropertyChanged();
        }

        #endregion

        #region property IsTransposed

        public bool IsTransposed
        {
            get { return (bool)this.GetValue(IsTransposedProperty); }
            set { this.SetValue(IsTransposedProperty, value); }
        }

        public static readonly DependencyProperty IsTransposedProperty = DependencyProperty.Register(
            "IsTransposed", typeof(bool), typeof(FastGridControl), new PropertyMetadata(false, OnIsTransposedPropertyChanged));

        private static void OnIsTransposedPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((FastGridControl)dependencyObject).OnIsTransposedPropertyChanged();
        }

        #endregion

        #region property PreciseCharacterGlyphs

        public bool PreciseCharacterGlyphs
        {
            get { return (bool)this.GetValue(PreciseCharacterGlyphsProperty); }
            set { this.SetValue(PreciseCharacterGlyphsProperty, value); }
        }

        public static readonly DependencyProperty PreciseCharacterGlyphsProperty = DependencyProperty.Register(
            "PreciseCharacterGlyphs", typeof(bool), typeof(FastGridControl), new PropertyMetadata(false, OnPreciseCharacterGlyphsPropertyChanged));

        private static void OnPreciseCharacterGlyphsPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((FastGridControl)dependencyObject).OnPreciseCharacterGlyphsPropertyChanged();
        }


        #endregion

    }
}