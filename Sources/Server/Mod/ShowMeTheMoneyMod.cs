using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SwiftXP.SPT.ShowMeTheMoney.Server.Http;

namespace SwiftXP.SPT.ShowMeTheMoney.Server;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.Preload + 1)]

#pragma warning disable CS9113 // Parameter is unread.
public class ShowMeTheMoneyMod(ModHttpListener httpListener)
#pragma warning restore CS9113 // Parameter is unread.
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
