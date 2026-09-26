import { ApiProperty } from '@nestjs/swagger';
import { IsNotEmpty, IsString, MinLength } from 'class-validator';

export class ChangePasswordDto {
  @ApiProperty({ example: 'OldPassword123!', description: 'Mật khẩu hiện tại' })
  @IsString()
  @IsNotEmpty({ message: 'Mật khẩu hiện tại không được để trống' })
  oldPassword: string;

  @ApiProperty({ example: 'NewPassword123!', description: 'Mật khẩu mới tối thiểu 6 ký tự' })
  @IsString()
  @MinLength(6, { message: 'Mật khẩu mới phải chứa ít nhất 6 ký tự' })
  newPassword: string;
}
