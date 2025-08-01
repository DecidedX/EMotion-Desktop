using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace EMotion
{
    internal class ControllerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ControllerDefault { get; set; }
        public DataTemplate? ControllerAdd { get; set; }
        public DataTemplate? ControllerInput { get; set; }
        public DataTemplate? QuickConnectAdd { get; set; }

        public override DataTemplate SelectTemplate(object i, DependencyObject container)
        {
            Item item = (Item) i;
            switch (item.Name)
            {
                case "add":
                    return ControllerAdd!;
                case "input":
                    return ControllerInput!;
                case "quick":
                    return QuickConnectAdd!;
                default:
                    return ControllerDefault!;
            }
        }

    }

    internal class Item
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }

}
