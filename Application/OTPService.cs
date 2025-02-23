using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Authentication.Domain.Repositories;

namespace Authentication.Application
{
    public class OTPService
    {
        private readonly IUserRepository _userRepo;
        private static ConcurrentDictionary<string, (string otp, DateTime expiry)> otpStore = new();

        public OTPService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<string?> GenerateOTPAsync(string phoneNumber, int length = 6)
        {
            var user = await _userRepo.GetUserByPhoneNumber(phoneNumber);
            if (user == null)
            {
                return null; // User not found, return null or throw an exception
            }

            var otp = new Random().Next(0, (int)Math.Pow(10, length)).ToString($"D{length}");
            otpStore[phoneNumber] = (otp, DateTime.UtcNow.AddMinutes(5)); // Expires in 5 minutes

            // Simulate sending OTP (replace with an actual SMS API)
            Console.WriteLine($"OTP for {phoneNumber}: {otp}");

            return otp;
        }






        // 2FA-specific OTP generation
        public async Task<string?> GenerateTwoFactorCodeAsync(string userId, int length = 6)
        {
            var user = await _userRepo.GetUserByPhoneNumber(userId);
            if (user == null)
            {
                return null;
            }

            var otp = new Random().Next(0, (int)Math.Pow(10, length)).ToString($"D{length}");
            otpStore[userId] = (otp, DateTime.UtcNow.AddMinutes(5));

            // Simulate sending OTP
            Console.WriteLine($"2FA OTP for user {userId}: {otp}");

            return otp;
        }

        public async Task<bool> ValidateTwoFactorCodeAsync(string userId, string inputOtp)
        {
            if (otpStore.TryGetValue(userId, out var storedOtp) && storedOtp.expiry > DateTime.UtcNow)
            {
                if (storedOtp.otp == inputOtp)
                {
                    otpStore.TryRemove(userId, out _);
                    return true;
                }
            }

            return false;
        }
    }
}
