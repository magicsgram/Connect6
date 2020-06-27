using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Connect6.Client
{
  public class BrowserResizeService
  {
    public static event Func<Task> OnResize;

    [JSInvokable]
    public static async Task OnBrowserResize()
    {
      await OnResize?.Invoke();
    }
  }
}