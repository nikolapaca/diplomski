import { Component, EventEmitter, Inject, Output } from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogModule, MatDialogRef} from '@angular/material/dialog';
import { UserService } from '../../service/user-service';
import { MatFormFieldModule} from '@angular/material/form-field';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TextFieldModule } from '@angular/cdk/text-field';
import { Board } from '../../model/board.model';
import { BoardStatus } from '../../model/board-status.enum';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-create-board-dialog',
  imports: [MatDialogModule,
    MatButtonModule,
    MatInputModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    TextFieldModule],
  templateUrl: './create-board-dialog.html',
  styleUrl: './create-board-dialog.css'
})
export class CreateBoardDialog {
  public username: string = '';
  public createBoardForm: FormGroup;
  public errorMessage: string = '';
  @Output() public boardSubmitted = new EventEmitter<Board>();

  public constructor(@Inject(MAT_DIALOG_DATA) public data: {name: string}, 
                      public dialog: MatDialogRef<CreateBoardDialog>, 
                      private userService: UserService) {
    this.createBoardForm = new FormGroup({
      boardName: new FormControl('', [Validators.required, Validators.maxLength(50)]),
      boardDescription: new FormControl('', Validators.maxLength(255))
    });
    this.username = this.userService.getUsername();
  }

  public setBackendError(message: string): void {
    this.createBoardForm.get('boardName')?.setErrors({
      'backendError': message
    });
    this.errorMessage = message;
  }

  public onSubmit(): void {
    const board: Board = {
      name: this.createBoardForm.value.boardName,
      description: this.createBoardForm.value.boardDescription,
      status: BoardStatus.ACTIVE,
      ownerUsername: ''
    }
    this.boardSubmitted.emit(board);
  }
}
