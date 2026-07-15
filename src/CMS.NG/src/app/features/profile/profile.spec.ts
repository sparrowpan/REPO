import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { AuthService } from '@core/services/auth.service';
import { Profile } from './profile';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const STORAGE_KEY = 'cms-auth';

function makeJwt(payload: Record<string, unknown>): string {
  const enc = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${enc({ alg: 'HS256', typ: 'JWT' })}.${enc(payload)}.sig`;
}

/** Seed session storage with a signed-in profile (userId 'helen') carrying the given roles. */
function signIn(roles: string[]): void {
  const token = makeJwt({ userId: 'helen', userName: 'Helen Wang', [ROLE_CLAIM]: roles });
  sessionStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({ userId: 'helen', userName: 'Helen Wang', accessToken: token }),
  );
}

describe('Profile', () => {
  let httpMock: HttpTestingController;
  const profileUrl = `${environment.apiBaseUrl}/Auth/profile`;

  beforeEach(async () => {
    sessionStorage.clear();
    signIn(['Admin', 'User']);
    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  interface PwControl {
    setValue: (v: string) => void;
    hasError: (code: string) => boolean;
  }
  interface PwForm {
    controls: {
      currentPassword: PwControl;
      newPassword: PwControl;
      confirmNewPassword: PwControl;
    };
    invalid: boolean;
    hasError: (code: string) => boolean;
    setValue: (v: {
      currentPassword: string;
      newPassword: string;
      confirmNewPassword: string;
    }) => void;
  }

  function createComponent() {
    const fixture = TestBed.createComponent(Profile);
    fixture.detectChanges();
    const instance = fixture.componentInstance as unknown as {
      form: { controls: { userName: { setValue: (v: string) => void } } };
      passwordForm: PwForm;
      save: () => void;
      changePassword: () => void;
    };
    return { fixture, instance };
  }

  const changePasswordUrl = `${environment.apiBaseUrl}/Auth/change-password`;

  it('creates', () => {
    const { fixture } = createComponent();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows UserId read-only', () => {
    const { fixture } = createComponent();
    const el = fixture.nativeElement as HTMLElement;
    const userIdInput = el.querySelector<HTMLInputElement>('#userId');

    expect(userIdInput).not.toBeNull();
    expect(userIdInput!.value).toBe('helen');
    // Read-only: not editable and not bound to a form control.
    expect(userIdInput!.hasAttribute('readonly')).toBe(true);
    expect(userIdInput!.getAttribute('formControlName')).toBeNull();
  });

  it('shows the roles as read-only display', () => {
    const { fixture } = createComponent();
    const roleText = (fixture.nativeElement as HTMLElement).querySelector('.role-tags')?.textContent ?? '';

    expect(roleText).toContain('Admin');
    expect(roleText).toContain('User');
    // No editable control for roles.
    expect((fixture.nativeElement as HTMLElement).querySelector('[formControlName="roles"]')).toBeNull();
  });

  it('saves the new UserName and refreshes the shell (session + signal)', () => {
    const { instance } = createComponent();
    instance.form.controls.userName.setValue('Helen Chang');
    instance.save();

    const req = httpMock.expectOne(profileUrl);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ userName: 'Helen Chang' });
    req.flush({ userId: 'helen', userName: 'Helen Chang' });

    // The app shell binds to auth.userName(); both it and session storage must reflect the new name.
    expect(TestBed.inject(AuthService).userName()).toBe('Helen Chang');
    const stored = JSON.parse(sessionStorage.getItem(STORAGE_KEY)!) as { userName: string };
    expect(stored.userName).toBe('Helen Chang');
  });

  it('trims the UserName before sending', () => {
    const { instance } = createComponent();
    instance.form.controls.userName.setValue('  Helen Chang  ');
    instance.save();

    const req = httpMock.expectOne(profileUrl);
    expect(req.request.body).toEqual({ userName: 'Helen Chang' });
    req.flush({ userId: 'helen', userName: 'Helen Chang' });
  });

  it('does not call the API when the UserName is blank', () => {
    const { instance } = createComponent();
    instance.form.controls.userName.setValue('   ');
    instance.save();

    expect(() => httpMock.expectNone(profileUrl)).not.toThrow();
  });

  describe('change password', () => {
    it('does not call the API when any field is empty', () => {
      const { instance } = createComponent();
      instance.passwordForm.setValue({
        currentPassword: '',
        newPassword: 'NewPass123',
        confirmNewPassword: 'NewPass123',
      });
      instance.changePassword();

      expect(instance.passwordForm.invalid).toBe(true);
      expect(() => httpMock.expectNone(changePasswordUrl)).not.toThrow();
    });

    it('rejects a new password shorter than 8 characters', () => {
      const { instance } = createComponent();
      instance.passwordForm.setValue({
        currentPassword: 'secret123',
        newPassword: 'Ab1!',
        confirmNewPassword: 'Ab1!',
      });
      instance.changePassword();

      expect(instance.passwordForm.controls.newPassword.hasError('complexity')).toBe(true);
      expect(() => httpMock.expectNone(changePasswordUrl)).not.toThrow();
    });

    it('rejects a new password using fewer than 3 character classes', () => {
      const { instance } = createComponent();
      // 12 lowercase letters: long enough but only one character class.
      instance.passwordForm.setValue({
        currentPassword: 'secret123',
        newPassword: 'abcdefghijkl',
        confirmNewPassword: 'abcdefghijkl',
      });
      instance.changePassword();

      expect(instance.passwordForm.controls.newPassword.hasError('complexity')).toBe(true);
      expect(() => httpMock.expectNone(changePasswordUrl)).not.toThrow();
    });

    it('accepts a new password with exactly 3 of the 4 character classes', () => {
      const { instance } = createComponent();
      // upper + lower + digit = 3 classes, length 10.
      instance.passwordForm.setValue({
        currentPassword: 'secret123',
        newPassword: 'NewPass123',
        confirmNewPassword: 'NewPass123',
      });

      expect(instance.passwordForm.controls.newPassword.hasError('complexity')).toBe(false);
      expect(instance.passwordForm.invalid).toBe(false);
    });

    it('rejects when confirmation does not match the new password', () => {
      const { instance } = createComponent();
      instance.passwordForm.setValue({
        currentPassword: 'secret123',
        newPassword: 'NewPass123',
        confirmNewPassword: 'NewPass124',
      });
      instance.changePassword();

      expect(instance.passwordForm.hasError('mismatch')).toBe(true);
      expect(() => httpMock.expectNone(changePasswordUrl)).not.toThrow();
    });

    it('posts current + new + confirm passwords when the form is valid', () => {
      const { instance } = createComponent();
      instance.passwordForm.setValue({
        currentPassword: 'secret123',
        newPassword: 'NewPass123',
        confirmNewPassword: 'NewPass123',
      });
      instance.changePassword();

      const req = httpMock.expectOne(changePasswordUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        currentPassword: 'secret123',
        newPassword: 'NewPass123',
        confirmNewPassword: 'NewPass123',
      });
      // No password hash is ever sent from the client.
      expect(JSON.stringify(req.request.body)).not.toContain('Hash');
      req.flush(null);
    });
  });
});
