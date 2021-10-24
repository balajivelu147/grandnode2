using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.System.Interfaces.ScheduleTasks;
using MediatR;
//using Microsoft.AspNet.OData;
//using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Threading.Tasks;

using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Blogs;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Grand.Api.Controllers.OData
{
    public partial class BlogController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;


        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IDateTimeService _dateTimeService;
        public BlogController(IMediator mediator, IPermissionService permissionService,
             IBlogService blogService,
            IBlogViewModelService blogViewModelService,
            ILanguageService languageService,
            ITranslationService translationService,
            IStoreService storeService,
            IWorkContext workContext,
            IGroupService groupService,
            IDateTimeService dateTimeService,
            IPictureViewModelService pictureViewModelService,
            SeoSettings seoSettings)
        {
            _mediator = mediator;
            _permissionService = permissionService;
            _blogService = blogService;
            _blogViewModelService = blogViewModelService;
            _languageService = languageService;
            _translationService = translationService;
            _storeService = storeService;
            _workContext = workContext;
            _groupService = groupService;
            _dateTimeService = dateTimeService;
            _pictureViewModelService = pictureViewModelService;
            _seoSettings = seoSettings;
        }


        #region Fields

        private readonly IBlogService _blogService;
        private readonly IBlogViewModelService _blogViewModelService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        //private readonly IDateTimeService _dateTimeService;
        private readonly IPictureViewModelService _pictureViewModelService;
        private readonly SeoSettings _seoSettings;

        #endregion 

   
         

        //public IActionResult Index() => RedirectToAction("List");

        //public IActionResult List() => View();

        [SwaggerOperation(summary: "Get entity from B1rand11 by key", OperationId = "GetBl11ogById")]
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> GetBlogList([FromBody]DataSourceRequest command)
        {

            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            if (ModelState.IsValid)
            {
            var blogPosts = await _blogViewModelService.PrepareBlogPostsModel(command.Page, command.PageSize);
                return Ok(blogPosts);
            }             
            return BadRequest(ModelState);
        }


        [SwaggerOperation(summary: "Get entity 1from B1rand11 by key", OperationId = "Get1Bl11ogById")]
        [Route("[action]")]
        [HttpGet]
        public async Task<IActionResult> GetBlogListById(string id)
        {

            if (!await _permissionService.Authorize(PermissionSystemName.Brands))
                return Forbid();

            if (ModelState.IsValid)
            {
                var blogPost = await _blogService.GetBlogPostById(id);

                return Ok(blogPost);
            }
            return BadRequest(ModelState);
        }

            [SwaggerOperation(summary: "Get enti11ty from B1rand11 by key", OperationId = "Get11Bl11ogById")]
        //[PermissionAuthorizeAction(PermissionActionName.Edit)]
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody]BlogPostModel model)
        {
            var blogPost = await _blogService.GetBlogPostById(model.Id);
            if (blogPost == null)
                //No blog post found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!blogPost.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return RedirectToAction("Edit", new { id = blogPost.Id });
            }

            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }

                blogPost = await _blogViewModelService.UpdateBlogPostModel(model, blogPost);
            } 
            return Ok(blogPost);//View(model);
        }



        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var blogPost = await _blogService.GetBlogPostById(id);
            if (blogPost == null)
                //No blog post found with the specified id
                return Ok();//RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                if (!blogPost.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                    return Ok();//RedirectToAction("Edit", new { id = blogPost.Id });
            }

            if (ModelState.IsValid)
            {
                await _blogService.DeleteBlogPost(blogPost);

                //Success(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Deleted"));
                return Ok();// RedirectToAction("List");
            }
            //Error(ModelState);
            return null;// RedirectToAction("Edit", new { id = id });
        }


        //[PermissionAuthorizeAction(PermissionActionName.Edit)]
        [Route("[action]")]
        [HttpPost]//, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create([FromBody]BlogPostModel model )
        {
            if (ModelState.IsValid)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    model.Stores = new string[] { _workContext.CurrentCustomer.StaffStoreId };
                }
                var blogPost = await _blogViewModelService.InsertBlogPostModel(model);
                //Success(_translationService.GetResource("Admin.Content.Blog.BlogPosts.Added"));
                return Ok(model);// continueEditing ? RedirectToAction("Edit", new { id = blogPost.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
           // ViewBag.AllLanguages = await _languageService.GetAllLanguages(true);

            return Ok(model);
        }





        [SwaggerOperation(summary: "Get entity from B1rand by key", OperationId = "GetBlogById")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var Blog = await _scheduleTaskService.GetTaskById(id);
            //var model = task.ToModel(_dateTimeService);
            //model = await PrepareStores(model);
            return Ok(null); //View(model);
        }

    }
}
