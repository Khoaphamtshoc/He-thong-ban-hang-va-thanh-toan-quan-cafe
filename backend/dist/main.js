import { NestFactory } from '@nestjs/core';
import { Logger, ValidationPipe } from '@nestjs/common';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';
import { AppModule } from './app.module.js';
import { HttpExceptionFilter } from './common/filters/http-exception.filter.js';
import { TransformInterceptor } from './common/interceptors/transform.interceptor.js';
async function bootstrap() {
    const logger = new Logger('Bootstrap');
    const app = await NestFactory.create(AppModule);
    app.enableCors({
        origin: true,
        credentials: true,
        methods: 'GET,HEAD,PUT,PATCH,POST,DELETE,OPTIONS',
    });
    const apiPrefix = process.env.API_PREFIX || 'api/v1';
    app.setGlobalPrefix(apiPrefix);
    app.useGlobalPipes(new ValidationPipe({
        whitelist: true,
        transform: true,
        forbidNonWhitelisted: true,
        transformOptions: {
            enableImplicitConversion: true,
        },
    }));
    app.useGlobalFilters(new HttpExceptionFilter());
    app.useGlobalInterceptors(new TransformInterceptor());
    const swaggerConfig = new DocumentBuilder()
        .setTitle('Hệ thống Quản lý Bán hàng & Thanh toán (Cafe POS API)')
        .setDescription('Tài liệu API Backend chuyên nghiệp xây dựng bằng NestJS, Clean Architecture, RBAC & TypeORM')
        .setVersion('1.0.0')
        .addBearerAuth({
        type: 'http',
        scheme: 'bearer',
        bearerFormat: 'JWT',
        name: 'JWT',
        description: 'Nhập JWT Access Token vào đây',
        in: 'header',
    }, 'JWT-auth')
        .build();
    const document = SwaggerModule.createDocument(app, swaggerConfig);
    SwaggerModule.setup('api/docs', app, document, {
        swaggerOptions: {
            persistAuthorization: true,
        },
    });
    const port = process.env.PORT ?? 3000;
    await app.listen(port);
    logger.log(`🚀 Ứng dụng Backend đang chạy tại: http://localhost:${port}/${apiPrefix}`);
    logger.log(`📖 Tài liệu Swagger API tại: http://localhost:${port}/api/docs`);
}
await bootstrap();
//# sourceMappingURL=main.js.map