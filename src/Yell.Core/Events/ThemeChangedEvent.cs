using Prism.Events;
using Yell.Core.Models;

namespace Yell.Core.Events
{
    public sealed class ThemeChangedEvent : PubSubEvent<AppTheme>
    {
    }
}
