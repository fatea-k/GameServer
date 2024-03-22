
public class PWH
{
    // 设置哈希的工作因子，可能需要根据您的服务器性能调整此值
    private static int WorkFactor = 10;

    // 生成哈希密码的静态方法
    public static string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
    }

    // 验证密码的静态方法
    public static bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
    }
}