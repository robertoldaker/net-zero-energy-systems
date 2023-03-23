using Microsoft.AspNetCore.Mvc;

public class DefaultController : ControllerBase
{
    [Route(""), HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public RedirectResult RedirectToSwaggerUi()
    {
        return Redirect("/swagger");
    }
}