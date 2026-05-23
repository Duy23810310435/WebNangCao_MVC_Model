using FluentValidation;
using WebNangCao_MVC_Model.Models;
using Microsoft.Extensions.Localization; // 🚨 GỌI THƯ VIỆN DỊCH

namespace WebNangCao_MVC_Model.Validators
{
    // Class độc lập 1: LoginValidator
    public class LoginValidator : AbstractValidator<LoginViewModel>
    {
        // 🚨 BƠM PHIÊN DỊCH VIÊN VÀO CONSTRUCTOR
        public LoginValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage(x => localizer["ValRequireUsernameOrEmail"])
                .MaximumLength(50).WithMessage(x => localizer["ValUsernameTooLong"])
                .Matches(@"^\S+$").WithMessage(x => localizer["ValUsernameNoSpaces"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(x => localizer["ValRequirePassword"]);
        }
    }

    // Class độc lập 2: RegisterValidator
    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        // 🚨 BƠM PHIÊN DỊCH VIÊN VÀO CONSTRUCTOR
        public RegisterValidator(IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(x => localizer["ValRequireName"]);

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage(x => localizer["ValRequireUsername"])
                .MinimumLength(3).WithMessage(x => localizer["ValUsernameMinLength"]);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(x => localizer["ValRequireEmail"])
                .EmailAddress().WithMessage(x => localizer["ValInvalidEmail"]);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(x => localizer["ValRequirePassword"])
                .MinimumLength(6).WithMessage(x => localizer["ValPasswordMinLength"]);

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage(x => localizer["ValPasswordMismatch"]);
        }
    }
}