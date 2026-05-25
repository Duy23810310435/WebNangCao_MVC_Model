using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Threading.Tasks;
using WebNangCao_MVC_Model.Data;
using Microsoft.EntityFrameworkCore;

namespace WebNangCao_MVC_Model.Middlewares
{
    public class GlobalLanguageMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalLanguageMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Context và DbContext được inject trực tiếp vào hàm Invoke
        //public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        //{
        //    // 1. Múc cấu hình từ DB lên
        //    var config = await dbContext.SystemConfigs.FirstOrDefaultAsync();

        //    // 2. Lấy mã ngôn ngữ (Mặc định cho vi-VN nếu DB rỗng)
        //    string cultureCode = config?.DefaultLanguage ?? "vi-VN";

        //    // 3. Ép kiểu văn hóa (Culture) cho cái luồng (Thread) đang chạy hiện tại
        //    try 
        //    {
        //        var culture = new CultureInfo(cultureCode);
        //        CultureInfo.CurrentCulture = culture;
        //        CultureInfo.CurrentUICulture = culture;
        //    }
        //    catch 
        //    {
        //        // Đề phòng DB lưu mã vớ vẩn không đúng chuẩn, thì fallback về tiếng Việt
        //        var fallbackCulture = new CultureInfo("vi-VN");
        //        CultureInfo.CurrentCulture = fallbackCulture;
        //        CultureInfo.CurrentUICulture = fallbackCulture;
        //    }

        //    // 4. Cho phép luồng đi tiếp vào Controller của em
        //    await _next(context);
        //}
        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            try
            {
                // 1. Múc cấu hình từ DB lên
                var config = await dbContext.SystemConfigs
                    .FirstOrDefaultAsync(context.RequestAborted); // Truyền CancellationToken

                // 2. Lấy mã ngôn ngữ
                string cultureCode = config?.DefaultLanguage ?? "vi-VN";

                // 3. Ép culture cho thread hiện tại
                try
                {
                    var culture = new CultureInfo(cultureCode);
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                }
                catch
                {
                    var fallbackCulture = new CultureInfo("vi-VN");
                    CultureInfo.CurrentCulture = fallbackCulture;
                    CultureInfo.CurrentUICulture = fallbackCulture;
                }

                // 4. Cho phép luồng đi tiếp
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // Client tự ngắt kết nối → bỏ qua, không throw
                // Không cần làm gì thêm
            }
            catch (IOException ex) when (ex.Message.Contains("reset"))
            {
                // Client reset stream → bỏ qua
            }
        }
    }
}