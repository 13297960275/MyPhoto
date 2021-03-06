﻿using JPager.Net;
using MyPhotos.DAL;
using MyPhotos.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyPhotos.Common;

namespace MyPhotos.Controllers
{
    [Authorize]
    //[AllowAnonymous]
    public class PhotosController : Controller
    {
        private BaseDBContext db = new BaseDBContext();
        private MD5Helper md5 = new MD5Helper();

        /// <summary>
        /// 分页显示图片列表
        /// </summary>
        /// <param name="param">继承了PagerInBase的Photos</param>
        /// <returns></returns>
        public ActionResult PagerIndex(PhotosParams param)
        {
            //每页显示的条数默认10
            param.PageSize = 12;

            //保存搜索条件
            //ViewBag.SearchUserName = param.UserName;
            //ViewBag.SearchRegisterTime = param.RegistrationTime;
            //ViewBag.SearchRoleName = param.Roles.RoleName;

            //获取数据集合
            //var list = PageContent();
            var list = db.Photos.Include(u => u.PhotoTypes).ToList();
            //var list = new List<User>();

            //根据条件检索
            var query = param._purl != null ? list.Where(t => t._purl.Contains(param._purl)).ToList() : list.ToList();

            //分页数据
            var data = query.Skip(param.Skip).Take(param.PageSize);

            //总页数
            var count = query.Count;

            var res = new PagerResult<Photos>
            {
                Code = 0,
                DataList = data,
                Total = count,
                PageSize = param.PageSize,
                PageIndex = param.PageIndex,
                RequestUrl = param.RequetUrl
            };
            return View(res);
        }

        /// <summary>
        /// GET:_AddNewPartialPage
        /// </summary>
        /// <returns></returns>
        public ActionResult AddNew()
        {
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename");
            return PartialView("_AddNewPartialPage");
        }

        /// <summary>
        /// POST:Upload photos
        /// </summary>
        /// <param name="uploadFile"></param>
        /// <param name="photos"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddNew(HttpPostedFileBase uploadFile, [Bind(Include = "_pid,_ptypeid,_ptitle,_purl,_pdes,_ptime,_pclicks,_pdownload,_pup,_pdown")] Photos photos)
        {
            //var pts = db.PhotoTypes.Where(pt => pt._typeid == photos._ptypeid);
            PhotoType photoTypes = db.PhotoTypes.Find(photos._ptypeid);
            if (ModelState.IsValid)
            {
                if (uploadFile != null && uploadFile.ContentLength > 0)//判断是否存在文件
                {
                    //暂不允许上传GIF || uploadFile.ContentType == "image/gif"
                    if (uploadFile.ContentType == "image/jpeg")//判断是否是图片文件 
                    {
                        /*采用MD5识别相同文件，防止重复上传*/
                        string fileMD5 = md5.GetStreamMD5(uploadFile.InputStream);
                        //Photos p = db.Photos.SingleOrDefault(dp => dp.MD5 == fileMD5);//若多个满足则SingleOrDefault引发异常
                        var ps = db.Photos.Where(dp => dp.MD5 == fileMD5).ToList();
                        //var p = ps.ToList()[0];
                        if (ps.Count() == 0)
                        {
                            string path = Server.MapPath("~/Images/");
                            string oldname = uploadFile.FileName;
                            string newname = Guid.NewGuid().ToString() + Path.GetExtension(oldname);
                            //string filetype = filedata.ContentType;
                            //int filesize = filedata.ContentLength;
                            uploadFile.SaveAs(Path.Combine(path, newname));
                            photos._purl = newname;
                            photoTypes._tcover = newname;
                            photos._pdownload = 0;
                            photos._pclicks = 0;
                            photos._pdown = 0;
                            photos._pup = 0;
                            photos._ptime = DateTime.Now;
                            photos.MD5 = fileMD5;
                            db.Entry(photoTypes).State = EntityState.Modified;//更新相册封面
                            db.Photos.Add(photos);
                            db.SaveChanges();
                        }
                        else
                        {
                            //photos._purl = ps[0]._purl;
                            HttpContext.Response.Write("<script>alert('已上传过这张图片，请重新选择图片上传！');</script>");
                            HttpContext.Response.Redirect("PagerIndex");
                            //HttpContext.Response.Flush();
                            //HttpContext.Response.End();
                            //RedirectToAction("PagerIndex");
                        }
                        return RedirectToAction("PagerIndex");
                    }
                    //else HttpContext.Response.Write("<script>alert('请选择图片文件server');</script>");
                }
                //else HttpContext.Response.Write("<script>alert('请选择文件server');</script>");
            }
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename", photos._ptypeid);
            //return PartialView("_AddNewPartialPage", photos);
            return RedirectToAction("PagerIndex");
        }

        /// <summary>
        /// GET:Del photos
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Del(int id)
        {
            Photos photos = db.Photos.Find(id);
            db.Photos.Remove(photos);
            db.SaveChanges();
            try
            {
                string fileName = photos._purl;
                string filePath = Server.MapPath("/Images/" + fileName);
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists == true)
                {
                    System.IO.File.Delete(filePath);
                }
                else
                {
                    HttpContext.Response.Write("<script>alert('删除失败！请稍后重试。');</script>");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return RedirectToAction("PagerIndex");
        }

        /// <summary>
        /// POST:Download
        /// </summary>
        /// <param name="fileName"></param>
        //[HttpPost]
        public void Download(int id)
        {
            string fileName = db.Photos.Find(id)._purl;
            //客户端保存的文件名
            string filePath = Server.MapPath("/Images/" + fileName);//路径

            //Response.ContentType = "application/octet-stream";
            //Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            //Response.TransmitFile(filePath);

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists == true)
            {
                const long ChunkSize = 102400;//100K 每次读取文件，只读取100Ｋ，这样可以缓解服务器的压力
                byte[] buffer = new byte[ChunkSize];
                Response.Clear();
                FileStream iStream = System.IO.File.OpenRead(filePath);
                long dataLengthToRead = iStream.Length;//获取下载的文件总大小
                Response.ContentType = "application/octet-stream";
                //HttpUtility.UrlEncode(fileName)解决IE下载中文文件是文件名乱码问题
                //Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(fileName));
                Response.AddHeader("Content-Disposition", "attachment;  filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8));
                while (dataLengthToRead > 0 && Response.IsClientConnected)
                {
                    int lengthRead = iStream.Read(buffer, 0, Convert.ToInt32(ChunkSize));//读取的大小
                    Response.OutputStream.Write(buffer, 0, lengthRead);
                    Response.Flush();
                    dataLengthToRead = dataLengthToRead - lengthRead;
                }
                Update(id, 2);
                Response.Close();
            }
            else Response.Write("<script>alert('您所选择的文件不存在');</script>");
        }

        public ActionResult Update(int id, int type)
        {
            //Dictionary<string, object> dicResult = new Dictionary<string, object>();
            //int? count = 0;
            Photos photos = db.Photos.Find(id);
            switch (type)
            {
                case 1: photos._pclicks += 1; break;
                case 2: photos._pdownload += 1; photos._pclicks += 1; /*count = photos._pdownload;*/ break;
                case 3: photos._pup += 1; photos._pclicks += 1; break;
                case 4: photos._pdown += 1; photos._pclicks += 1; break;
                default: photos._pclicks += 1; break;
            }
            db.Entry(photos).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("PagerIndex");
        }

        #region EF自带和测试用Action

        /// <summary>
        /// FilePathResult方式下载
        /// </summary>
        /// <returns></returns>
        public FileResult GetFileByPath(int id)
        {
            string fileName = db.Photos.Find(id)._purl;
            //客户端保存的文件名
            string filePath = Server.MapPath("/Images/" + fileName);//路径
            List<string> typeList = new List<string>() { ".png", ".jpg", ".jpeg", ".gif", "bmp" };
            //string FileName = Path.GetFileName(FilePath);

            if (fileName != null)
            {
                if (typeList.Contains(Path.GetExtension(fileName)))
                {
                    return File(filePath, "image/jpg");
                }
                // 存在fileDownLoad属性的FileResult以附件形式下载，默认会以内联方式打开
                return File(filePath, "application/octet-stream", fileName);
            }
            return null;
        }

        /// <summary>
        /// GET:Upload
        /// </summary>
        /// <returns></returns>
        public ActionResult Upload()
        {
            //ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename");
            return View();
        }

        /// <summary>
        /// POST:Upload
        /// </summary>
        /// <param name="uploadFile">上传的文件</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase uploadFile/*, [Bind(Include = "_ptypeid")] Photos photos*/)
        {
            if (uploadFile != null && uploadFile.ContentLength > 0)
            {
                string path = Server.MapPath("~/Images/");
                string oldname = uploadFile.FileName;
                string newname = Guid.NewGuid().ToString() + Path.GetExtension(oldname);
                //string filetype = filedata.ContentType;
                //int filesize = filedata.ContentLength;
                uploadFile.SaveAs(Path.Combine(path, newname));
                return Content("成功");
            }
            return Content("失败");
        }

        /// <summary>
        ///  GET: Photos/Index
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var photos = db.Photos.Include(p => p.PhotoTypes);
            //ViewBag.url = HttpContext.Server.MapPath("");
            return View(photos.ToList());
        }

        /// <summary>
        /// GET:根据图片类型返回相册
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult GetPhotosByType(int id)
        {
            var photos = db.Photos.Where(p => p._ptypeid == id);
            ViewBag.ptypeName = db.PhotoTypes.Find(id)._typename;
            return View(photos.ToList());
        }

        /// <summary>
        /// GET: Photos/Details/5
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Photos photos = db.Photos.Find(id);
            if (photos == null)
            {
                return HttpNotFound();
            }
            return View(photos);
        }

        /// <summary>
        /// GET: Photos/Create
        /// </summary>
        /// <returns></returns>
        public ActionResult Create()
        {
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename");
            return View();
        }

        /// <summary>
        /// POST: Photos/Create
        /// </summary>
        /// <param name="uploadFile">上传的文件</param>
        /// <param name="photos">photos实体类</param>
        /// 为了防止“过多发布”攻击，请启用要绑定到的特定属性，有关 详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=317598。
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HttpPostedFileBase uploadFile, [Bind(Include = "_pid,_ptypeid,_ptitle,_purl,_pdes,_ptime,_pclicks,_pdownload,_pup,_pdown")] Photos photos)
        {
            /*采用MD5识别相同文件，防止重复上传，实现方法在此处添加*/
            //string fileMD5 = CommonFuncs.GetStreamMD5(Filedata.InputStream);

            if (ModelState.IsValid)
            {
                if (uploadFile != null && uploadFile.ContentLength > 0)//判断是否存在文件
                {
                    if (uploadFile.ContentType == "image/gif")//判断是否是图片文件
                    {
                        string path = Server.MapPath("~/Images/");
                        string oldname = uploadFile.FileName;
                        string newname = Guid.NewGuid().ToString() + Path.GetExtension(oldname);
                        //string filetype = filedata.ContentType;
                        //int filesize = filedata.ContentLength;
                        uploadFile.SaveAs(Path.Combine(path, newname));
                        photos._purl = newname;
                        photos._pdownload = 0;
                        photos._pclicks = 0;
                        photos._pdown = 0;
                        photos._pup = 0;
                        photos._ptime = DateTime.Now;
                        db.Photos.Add(photos);
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                        HttpContext.Response.Write("<script>alert('请选择图片文件server');</script>");
                }
                else
                    HttpContext.Response.Write("<script>alert('请选择文件server');</script>");
            }
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename", photos._ptypeid);
            return View(photos);
        }

        /// <summary>
        /// GET: Photos/Edit/5
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Photos photos = db.Photos.Find(id);
            if (photos == null)
            {
                return HttpNotFound();
            }
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename", photos._ptypeid);
            return View(photos);
        }

        /// <summary>
        /// POST: Photos/Edit/5
        /// </summary>
        /// 为了防止“过多发布”攻击，请启用要绑定到的特定属性，有关详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=317598。
        /// <param name="photos"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "_pid,_ptypeid,_ptitle,_purl,_pdes,_pclicks,_ptime,_pup,_pdown")] Photos photos)
        {
            if (ModelState.IsValid)
            {
                //photos._ptime = DateTime.Now;
                db.Entry(photos).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag._ptypeid = new SelectList(db.PhotoTypes, "_typeid", "_typename", photos._ptypeid);
            return View(photos);
        }

        /// <summary>
        /// GET: Photos/Delete/5
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Photos photos = db.Photos.Find(id);
            if (photos == null)
            {
                return HttpNotFound();
            }
            return View(photos);
        }

        /// <summary>
        /// POST: Photos/Delete/5
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Photos photos = db.Photos.Find(id);
            db.Photos.Remove(photos);
            db.SaveChanges();
            string fileName = photos._purl;
            string filePath = Server.MapPath("/Images/" + fileName);
            System.IO.File.Delete(filePath);
            return RedirectToAction("PagerIndex");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
