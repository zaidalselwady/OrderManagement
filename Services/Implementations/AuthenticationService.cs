
using global::Order_Management_System.Models.DTOs;

using Order_Management_System.Repositories.Interfaces;

using Order_Management_System.Services.Interfaces;

namespace Order_Management_System.Services.Implementations;



    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IUserRepository userRepository, ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LoginResultDto> AuthenticateAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return new LoginResultDto
                    {
                        Success = false,
                        ErrorMessage = "Username and password are required",
                        ErrorType = "INVALID_INPUT"
                    };
                }

                _logger.LogInformation("Authentication attempt for username: {Username}", username);

                var user = await _userRepository.GetUserByCredentialsAsync(username, password);

                if (user == null)
                {
                    // Check if username exists to provide specific error
                    bool userExists = await _userRepository.IsUserExistsAsync(username);

                    _logger.LogWarning("Authentication failed for username: {Username}", username);

                    return new LoginResultDto
                    {
                        Success = false,
                        ErrorMessage = userExists ? "Invalid password" : "Username not found",
                        ErrorType = userExists ? "INCORRECT_PASSWORD" : "USER_NOT_FOUND"
                    };
                }

                // Check password expiration
                var passwordExpiryCheck = CheckPasswordExpiry(user);
                if (!passwordExpiryCheck.IsValid)
                {
                    return new LoginResultDto
                    {
                        Success = false,
                        ErrorMessage = passwordExpiryCheck.ErrorMessage,
                        ErrorType = "PASSWORD_EXPIRED"
                    };
                }

                // Validate company association
                if (!user.CompanyId.HasValue)
                {
                    _logger.LogWarning("No company associated with user: {Username}", username);
                    return new LoginResultDto
                    {
                        Success = false,
                        ErrorMessage = "No company associated with this user. Please contact support.",
                        ErrorType = "NO_COMPANY_ID"
                    };
                }

                _logger.LogInformation("Authentication successful for user: {Username}", username);

                return new LoginResultDto
                {
                    Success = true,
                    UserId = user.UserId,
                    UserName = user.UserName,
                    CompanyId = user.CompanyId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for username: {Username}", username);
                return new LoginResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred during authentication. Please try again.",
                    ErrorType = "SERVER_ERROR"
                };
            }
        }

        public async Task<bool> IsUserLoggedInAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is logged in for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<string> GetUserNameAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                return user?.UserName ?? "Unknown User";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username for userId: {UserId}", userId);
                return "Unknown User";
            }
        }

        private (bool IsValid, string ErrorMessage) CheckPasswordExpiry(Models.Domain.User user)
        {
            if (user.PasswordDate == null || !user.PasswordPeriod.HasValue || user.PasswordPeriod.Value <= 0)
            {
                // No expiry policy set
                return (true, string.Empty);
            }

            DateTime expiryDate = user.PasswordDate.Value.AddDays(user.PasswordPeriod.Value);

            if (DateTime.Now > expiryDate)
            {
                _logger.LogWarning("Password expired for user: {Username}", user.UserName);
                return (false, "Your password has expired. Please reset your password.");
            }

            return (true, string.Empty);
        }
    }
