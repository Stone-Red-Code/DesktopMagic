using System;

namespace DesktopMagicPluginAPI.Inputs
{
    /// <summary>
    /// Marks a Property as element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ElementAttribute : Attribute
    {
        /// <summary>
        /// The name of the element.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The order index of the element.
        /// </summary>
        public int OrderIndex { get; }

        /// <summary>
        /// Marks a Property as element with the provided <paramref name="name"/> and <paramref name="orderIndex"/>.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="orderIndex">The order index of the element.</param>
        public ElementAttribute(string name = "", int orderIndex = 0)
        {
            Name = name;
            OrderIndex = orderIndex;
        }

        /// <summary>
        /// Marks a Property as element with the provided <paramref name="orderIndex"/>.
        /// </summary>
        /// <param name="orderIndex">The order index of the element.</param>
        public ElementAttribute(int orderIndex = 0)
        {
            OrderIndex = orderIndex;
        }

        /// <inheritdoc cref="ElementAttribute"/>
        public ElementAttribute()
        {
        }
    }
}